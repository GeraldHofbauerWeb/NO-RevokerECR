# NO-RevokerECR

A planned mod for Nuclear Option that adds the Revoker ECR, a variant of the FS-12 Revoker that players select separately in the hangar. The vanilla Revoker stays as it is.

Status: concept. The list of changes is still being written.

## Approach under consideration

The mod clones the vanilla Revoker at game start and registers the clone as an aircraft of its own: an Encyclopedia entry, a network prefab ID and a place in the hangars that offer the Revoker. The A-19J mod works the same way. Changes to values and systems (radar, emitter locator, weapons, flight performance) happen on the clone. Visible model changes would need a [Blueprinter](https://github.com/nikkorap/NOBlueprinter-Releases) asset bundle on top.

Every player in a multiplayer session needs the same version of the mod.
