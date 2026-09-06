# Gameplay Tags

Hierarchical gameplay tags for Unity, in the spirit of Unreal's `GameplayTag`. Tags are
ScriptableObject assets arranged in a parent/child tree (`Damage.Fire`, `State.Stunned`),
so a query about `Damage` can be answered by anything under it.

## What's in the box

- **`GameplayTagSO`** — one asset per tag, holding its name, parent and children. Dotted
  full names are derived from the hierarchy.
- **`GameplayTagSet`** — a serializable set of tags. `HasTag` / `HasTagAny` / `HasTagAll`
  are exact membership; the `HasParentOf*` / `HasChildOf*` family walks the hierarchy.
- **`GameplayTagMultiSet`** — a ref-counted set for when several sources grant the same tag
  (two effects both granting `Stunned` must not lose it when one expires).
  `UpdateTagCount` reports only real 0 ↔ positive transitions.
- **`IGameplayTagComponent`** — the contract a game entity implements so other systems can
  grant and revoke tags without depending on the entity type.
- **Editor**: a tag property drawer with fuzzy search, a `GameplayTagSet` drawer, and a
  **Tags Browser** window (`Window > GameplayTags > Tags Browser`, Ctrl/Cmd+Shift+T) for
  creating, renaming and deleting tags.

The package has no dependencies beyond Unity itself.

## Install

Unity 6000.0 or newer.

**From git, via the Package Manager manifest** — add to `Packages/manifest.json`:

```json
"com.alanvoid.gameplaytags": "https://github.com/alanaman/GameplayTags.git"
```

Pin a tag or commit with `#v1.0.0` / `#<sha>` at the end of the URL if you want a fixed version.

**As a git submodule** (editable in place):

```sh
git submodule add https://github.com/alanaman/GameplayTags.git Packages/com.alanvoid.gameplaytags
```

Unity treats a folder under `Packages/` with a `package.json` as an embedded package.

## Usage

Tag assets live in your project at `Assets/Resources/GameplayTags/` (the folder is created
on first use). Create them from the Tags Browser by typing a dotted name such as
`Damage.Fire`; each segment becomes an asset named `{FullName}_tag.asset`, with parents
created as needed. Segments may only contain letters and digits.

Reference tags from your own code with plain serialized fields:

```csharp
using GameplayTags;

public class Fireball : MonoBehaviour
{
    [SerializeField] GameplayTagSO damageType;   // picked with a searchable dropdown
    [SerializeField] GameplayTagSet grantedTags; // drawn as an editable list
}
```

Give your entity a tag surface by implementing `IGameplayTagComponent` on top of a
`GameplayTagMultiSet`; systems that grant tags call `UpdateTagCount(tags, +1)` and
`UpdateTagCount(tags, -1)` and never touch the set directly.

Tag discovery through `Resources.LoadAll` happens only in the editor. At runtime tags reach
code purely through serialized references, so there is no registry to initialise.

## Tests

Edit-mode tests live in `Tests/Editor` and show up in the Test Runner when the package is
embedded or listed under `testables` in your manifest.

## License

MIT — see [LICENSE.md](LICENSE.md).
