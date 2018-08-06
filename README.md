# SUMMARY

The primary purpose of this demo is to showcase the differences between level 1, 2, and 3 environment detection, as well as to demonstrate the use of the multiframe alignment UI. 

This demo allows users to localize themselves in Sturfee-enabled cities, and place digital objects on the ground and buildings.

# Levels

Your Sturfee API key determines what levels are available to use.
The levels available to you are those that are equal to and below your access level

**Level 1**

Detects terrain and buildings, but requires an API call for every environment detection check. Slower than level 2 and 3.
(If you have level 3 access, level 1 calls are automatically overwritten to use level 3)

**Level 2**

Uses terrain data loaded at the start of the application, allowing for faster and more detailed terrain detection. But does not detect buildings.

**Level 3**

Uses terrain and building data loaded at the start of application, allowing for faster and more detailed terrain and building detection.

# BUILD REQUIREMENTS

The default project has blank Mapbox and Sturfee keys.  
*You must plug in your own unique keys in order to use this sample game correctly.*

Go to [Mapbox.com](https://mapbox.com) and [Sturfee.com](https://sturfee.com) and create an account to generate your respective keys.

**How to apply Mapbox key:**

1. In the Unity project, on the top menu bar, click Mapbox -> Setup. A new window should appear.
2. Under "Access Token" paste your key value from the Mapbox website and press the "Submit" button

**How to apply Sturfee key:**

1. In the Unity project, on the top menu bar, click Sturfee -> Configure. A new window should appear.
2. Next to "API Key" there is an empty box. Paste your key value from the Sturfee website here and press the "Request Access" button

**Building to Android (With AR Core)**

1. Open the Unity project and click on the 'SturfeeXrSession' object in the Game scene hierarchy
2. Under the 'SturfeeXrSession' script in the Inspector view, click on the 'Provider Set' options. Make sure it is set to Custom -> ArCore Provider Set.
3. Then make sure that the 'Play On Start' option is toggled OFF

**Building to iPhone (With AR Kit)**

1. Open the Unity project and click on the 'SturfeeXrSession' object in the Game scene hierarchy
2. Under the 'SturfeeXrSession' script in the Inspector view, click on the 'Provider Set' options. Make sure it is set to Custom -> ArKit Provider Set
3. Then make sure that the 'Play On Start' option is toggled ON


# HOW TO PLAY:

### User Localization:
1. Go outside.
2. Check that your phone has GPS on with a good internet connection.
3. Open the app and wait for the Sturfee session to initialize.
4. Align your phone roughly perpendicular to the ground in landscape mode as the prompt tells you to hold your phone up.
4. Face your camera toward buildings, ideally toward a city skyline, then press the scan button as you keep the phone camera level.
5. Stand still and move your phone in order to align the center of the screen with the circles placed in the environment.
6. Wait for localization to complete as your location is computed using the pictures just taken.
6. Once localization is complete, take note of where you are located in the mini-map view in relation to your camera view. You can also press on the 'Map View' button in the top right to get a full screen view, allowing you to move the map camera around as well.
7. On the right hand side there are several buttons. Tap the 'Level 1 Placement' button, then tap on either the ground or a building to place an object at that location. 
8. This action calls the Sturfee server and might take a moment to register placement. A failed call could be the result of connection issues.
9. Now tap the 'Level 3 Placement' button, then drag your finger across the terrain and buildings on screen. Notice that the object can be freely dragged along these environments.
10. This action uses preloaded building and terrain data to determine placement. It does not require an API call for every environment check as level 1 does.
11. Determine a desirable location for the object, then press the 'Save Placement' button at the top.
12. Using Tier 2 Placement only takes in preloaded terrain data and does not hold building data. Thus it only enables ground placement.
13. You can also press the 'Remove Mode' button and then tap on any AR objects placed to be given the option to remove them.

___


>NOTES:
>
> This app has code turned off by default that saves the AR objects the user places by their GPS position. This means the user can close and reopen the app in a different location, and see the AR objects they placed still in the same real world location that they left them. This feature can be turned on by finding the GameManager object in the hierarchy and toggling 'AllowSaveLoad' on the GameManager script attached to it.
>
> Unity Version: 2017.3.0f3
>
