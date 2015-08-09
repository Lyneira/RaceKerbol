Race Kerbol
===========
About:
Race Kerbol (the sun) in a series of scenarios from start to finish, while trying to
keep as close to the ground as possible to get a higher score! Your craft is solar powered,
so make sure to keep your panels exposed to the sun as much as possible.

Dependencies: Kerbal Foundries, ModuleManager
Installation: Copy the saves and GameData directories into your KSP folder.

Quick Start: Go to Start Game, then Scenarios, pick a Race Kerbol scenario. Play!
Scoring: To be determined, but a higher land distance is better!

Extra: The Extra folder contains craft files for the Solar Racer, plus a Finishline
and Checkpoint craft for making your own scenarios.

= How to make a scenario =
* Create a fresh sandbox save and copy the Solar Racer.craft and optionally the
  checkpoint/finishline craft into it.
* Place your finishline and any checkpoints, remember to name them
  and set their craft type to Base. HyperEdit's Ship Lander strongly recommended.
* Move the Solar Racer to the starting point, either using hyperedit or by flying there.
  You can rightclick on an antigrav motor and set it to 0, then apply settings to put
  it on the ground for quicksaving while you experiment with the right starting time.
* Warp to the desired starting time, take off and fly into the starting situation and
  hit alt-F9 to make a named quicksave.
** Possibly useful: Edit the time in your savefile like so:
	FLIGHTSTATE
	{
		UT = 0.0
	}
	This sets the time back to year 1, day 0, hour 0.
* Copy your named quicksave .sfs file from your save to <KSPDir>\saves\scenarios
  and rename it. This name will show up in the Scenarios menu.
* Edit the .sfs and change the following parameters:
GAME
{
	Title = <your title>
	Description = <your description>
	PARAMETERS
	{
		FLIGHT
		{
			<optional> CanQuickSave = False
			<optional> CanQuickLoad = False
			CanAutoSave = False
			CanLeaveToEditor = False
			CanLeaveToTrackingStation = False
			CanLeaveToSpaceCenter = False
			CanLeaveToMainMenu = True
		}
	}
	FLIGHTSTATE
	{
		VESSEL
		{
			<Find solar racer vessel by searching for VESSEL or Solar Racer>
			met = 0.0
			lct = <find UT (universal time) and set to this value>
		}
	}

}

