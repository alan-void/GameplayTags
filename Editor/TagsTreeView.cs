using System.Collections.Generic;
using System.Linq;
using H2V.ExtensionsCore.Editor.Helpers;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GameplayTags.Editor
{
    public class TagTreeViewItem : TreeViewItem
    {
        public GameplayTagSO GameplayTag { get; set; }
    }

    public class TagsTreeView : TreeView
    {
        public TagsTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            showAlternatingRowBackgrounds = true;
            useScrollView = true;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            
            BuildTreeNodes(root, GameplayTagConfig.instance.RootTags);
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private void BuildTreeNodes(TreeViewItem parent, List<GameplayTagSO> tags)
        {
            parent.children = new List<TreeViewItem>();
            foreach (var tag in tags)
            {
                // if(!tag) continue;
                Assert.IsNotNull(tag, "Failed to find tag");
                var item = CreateTagTreeItem(tag);
                parent.AddChild(item);
                BuildTreeNodes(item, tag.ChildTags);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as TagTreeViewItem;
            var rect = args.rowRect;
            rect.xMin += GetContentIndent(item);
            EditorGUI.LabelField(rect, args.item.displayName);
            
            AddNewTagButton(rect, item);
        }

        private void AddNewTagButton(Rect rect, TagTreeViewItem item)
        {
            var buttonRect = new Rect(rect.xMax - 32, rect.y, 32, rect.height);
            if (GUI.Button(buttonRect, "+"))
            {
                var newTag = GameplayTagConfig.instance.AddTag(
                    item.GameplayTag.TagFullName + $".NewTag{item.GameplayTag.ChildTags.Count + 1}");
                Assert.IsTrue(newTag, "Failed to add new tag"); 
                AssetDatabase.SaveAssets();
                GameplayTagConfig.instance.ReloadAndValidateTags();
                // TODO: this is not working
                // var newItem = CreateTagTreeItem(newTag);
                // item.AddChild(newItem);
                // SetExpanded(item.id, true);
                // SetSelection(new List<int>() {newItem.id});
                // Reload();
                // Repaint();
            }
        }

        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var item = FindItem(id, rootItem) as TagTreeViewItem;
            if (item == null) return;
            ShowContextMenu(item);
        }
        private void ShowContextMenu(TagTreeViewItem tag)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Rename"), false, () => BeginRename(tag));
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                GameplayTagConfig.instance.DeleteTag(tag.GameplayTag);
                Reload();
            });

            menu.ShowAsContext();
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var item = FindItem(id, rootItem) as TagTreeViewItem;
            Selection.activeObject = item.GameplayTag;
        }

        protected override bool CanRename(TreeViewItem item) => true;
        protected override bool CanStartDrag(CanStartDragArgs args) => false;
        protected override bool CanBeParent(TreeViewItem item) => true;

        private const string _dragId = "ParentingDragId";
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (hasSearch)
                return;
 
            DragAndDrop.PrepareStartDrag();
            var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
            DragAndDrop.SetGenericData(_dragId, draggedRows);
            // DragAndDrop.objectReferences = new UnityEngine.Object[] { }; // this IS required for dragging to work
            string title = draggedRows.Count == 1 ? draggedRows[0].displayName : "< Multiple >";
            DragAndDrop.StartDrag(title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // Check if we can handle the current drag data (could be dragged in from other areas/windows
            if (!DragAndDrop.GetGenericData(_dragId).GetType().IsAssignableFrom(typeof(List<TreeViewItem>)))
                return DragAndDropVisualMode.None;

            if (!args.performDrop) return DragAndDropVisualMode.Move;
            // get the dragged rows
            var draggedRows = DragAndDrop.GetGenericData(_dragId) as List<TreeViewItem>;

            if (args.parentItem == null)
            {
                SetTagParent(draggedRows, null);
                return DragAndDropVisualMode.None;
            }
            // get the parent item
            var parentItem = FindItem(args.parentItem.id, rootItem) as TagTreeViewItem;
            if (parentItem.GameplayTag == null)
                return DragAndDropVisualMode.None;

            SetTagParent(draggedRows, parentItem.GameplayTag);
            
            return DragAndDropVisualMode.Move;
        }

        private void SetTagParent(List<TreeViewItem> draggedRows, GameplayTagSO parent)
        {
            bool assetChanged = false;
            foreach (var row in draggedRows)
            {
                var id = row.id;
                var tagSO = ((TagTreeViewItem) row).GameplayTag;
                if (row == null || tagSO == parent) continue;
                tagSO.SetPrivateProperty("_parent", parent);
                assetChanged = true;
            }
            if (!assetChanged) return;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Reload();
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            base.RenameEnded(args);
            
            var item = FindItem(args.itemID, rootItem) as TagTreeViewItem;
            Assert.IsNotNull(item, "Failed to find item");
            GameplayTagConfig.instance.RenameTag(item.GameplayTag, args.newName);
            AssetDatabase.Refresh();
            Reload();
        }

        private TagTreeViewItem CreateTagTreeItem(GameplayTagSO gameplayTag)
        {
            var item = new TagTreeViewItem
            {
                id = gameplayTag.GetEntityId().GetHashCode(),
                displayName = gameplayTag.TagName,
                GameplayTag = gameplayTag
            };
            return item;
        }
    }
}