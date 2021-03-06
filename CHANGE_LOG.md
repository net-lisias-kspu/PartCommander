# Part Commander :: Change Log

* 2018-1109: 1.1.5.2 (Lisias) for KSP 1.4.1+; 1.5
	+ Using KSPe Facilities
		- Logging
		- Config Data
		- Assets
* 2018-1020: 1.1.5.1 (linuxgurugamer) for KSP 1.5.1
	+ Version bump for 1.5 rebuild
* 2018-0728: 1.1.5 (linuxgurugamer) for KSP 1.4.4
	+ Added Alternate skin settings
	+ Added ability to change font size
* 2018-0727: 1.1.4.1 (linuxgurugamer) for KSP 1.4.4
	+ Updated .version file for all 1.4.x
* 2018-0421: 1.1.4 (linuxgurugamer) for KSP 1.4.2
	+ Fixed nullrefs when doing symmetry parts
	+ Added toolbar registration
	+ Removed old toolbar code
* 2018-0318: 1.1.3.3 (linuxgurugamer) for KSP 1.4.1
	+ Fix for missing events
* 2018-0318: 1.1.3.2 (linuxgurugamer) for KSP 1.4.1
	+ Fix for missing directories
* 2018-0318: 1.1.3.1 (linuxgurugamer) for KSP 1.4.1
	+ Fixed unresponsive button when window is opened by config
	+ Fixed nullref at startup
	+ Removed debugging statement
* 2018-0317: 1.1.3 (linuxgurugamer) for KSP 1.4.1
	+ Fixed localization strings being displayed in fields
	+ Added support for the ClickThroughBlocker
	+ Changed to allow dragging window from anywhere
	+ Added support for the ToolbarController
	+ Replaced most "foreach" with (for int i = ) loops for effeciency and memory conservation
* 2017-1011: 1.1.2 (linuxgurugamer) for KSP 1.3.1
	+ Updated for KSP 1.3.1
* 2017-0528: 0.1.2 (linuxgurugamer) for KSP 1.3.0
	+ Updated for 1.3
* 2016-1102: 0.1.1.1 (linuxgurugamer) for KSP 1.2.2
	+ No changelog provided
* 2016-1021: 0.1.1 (linuxgurugamer) for KSP 1.2
	+ Update per blizzy78/ksp_toolbar#39 to prevent NotSupportedException. …
* 2016-1016: 0.1.0 (linuxgurugamer) for KSP 1.2
	+ No changelog provided
* 2016-0507: 1.1.1 (seanmcdougall) for KSP 1.1.2
	+ recompiled for KSP 1.1.2
* 2016-0427: 1.1 (seanmcdougall) for KSP 1.1
	+ fixed and recompiled for KSP 1.1
* 2015-1120: 1.0.3 (seanmcdougall) for KSP 1.0.5
	+ recompiled for KSP 1.0.5
	+ fixes a bug where hiding window with keyboard shortcut failed to release control lock
* 2015-0731: 1.0.2.4 (seanmcdougall) for KSP 1.1
	+ fixes a NRE that was showing up in the logs
* 2015-0719: 1.0.2.3 (seanmcdougall) for KSP 1.1
	+ bug fixes to hover logic and part highlighting
* 2015-0709: 1.0.2.2 (seanmcdougall) for KSP 1.1
	+ another hotfix to correct an issue with settings not being saved properly to the persistent config file
* 2015-0709: 1.0.2.1 (seanmcdougall) for KSP 1.1
	+ hotfix to correct an issue where the part list wouldn't always update when parts were destroyed
* 2015-0709: 1.0.2 (seanmcdougall) for KSP 1.1
	+ added persistent PartCommander.cfg settings file which gets created under GameData/PartCommander
	+ created new Settings window to manage this file
	+ added optional support for blizzy78's Toolbar (http://forum.kerbalspaceprogram.com/threads/60863)
	+ stock toolbar button can be disabled through the new Settings window
	+ added hot key support for showing/hiding windows instead of using toolbar buttons.  Default key combo is Mod + P.
	+ This can be changed through the Settings window (but be careful not to pick something that KSP already uses).
	+ you can also disable the hot key altogether through Settings
	+ added a setting to hide "unactionable" parts... those that just have display fields but no buttons or sliders.
	+ made the tooltips more visible by setting a solid background colour
	+ fixed some control locking issues when mousing over a window
* 2015-0705: 1.0.1 (seanmcdougall) for KSP 1.1
	+ added .version file
	+ fixed a bug that prevented the parts list from updating properly when using the filtering/sorting options
	+ made the search field a toggleable option
	+ added a toggleable category filter
	+ added tooltips
	+ made the scrollbar position persistent when moving back and forth between the main listing and a part
* 2015-0704: 1.0.0 (seanmcdougall) for KSP 1.1
	+ First official release.
