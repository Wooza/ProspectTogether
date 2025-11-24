# Changelog
* 2.1.2
    * Add spanish translation
* 2.1.1
    * Players can toggle visibility of individual prospecting chunks
* 2.1.0
    * Update game version to 1.21.0
* 2.1.0-rc.1
    * Update to .NET 8
    * Update game version to 1.21.0-rc.4
* 2.0.7
    * Remove obsolete server option `SharingAllowed` and `/setsharingallowed`. Don't install the mod on the server if users should not be allowed share.
* 2.0.6
    * Don't render prospecting data for chunks which are outside of the current map view
    * Only process new or changed prospecting data received from the in-game mechanism
* 2.0.5
    * Remove dependency on latest game version (again)
* 2.0.4
    * Build against latest game version
* 2.0.3
    * Clean up settings dialog
* 2.0.2
    * Fix game crash due to null dereference exception
    * Cleanup Harmony Patching
* 2.0.1
    * Remove game version dependency, as seems to cause problems
* 2.0.0
    * ProspectTogether can now be used client-side only. Sharing still requires mod installation on the server.
* Older versions
    * See release notes of that version