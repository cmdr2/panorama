## panorama

fast, infinite panoramas from flickr for Google Cardboard using Unity 5.2

feel free to contribute or fork to create your own panoramas application for personal or commercial use.

**note:** if you seriously intend to fork this for your own purposes, let me know so i can clean up the code a bit to make it easier to read. either upvote [this task](https://github.com/cmdr2/panorama/issues/1) or use my contact at the bottom.

## features
  * supports equirectangular projection panoramas
  * uses a high-poly sphere to confine image distortion to a narrow range around the poles (± 4°)
  * progressive loading of image by resolution to speed up time-to-first-pixel
  * provides control to turn panorama by 180° to help seated users look behind them
  * provides control to load a random new panorama
  * includes Google Analytics with anonymized IPs to help monitor errors and application performance in the real world
  * displays title and author, and autohides below users' feet
  * example datasource fetches random panoramas from flickr
  * flickr contains >14k equirectangular panoramas, so ~2 years worth of fresh panoramas for casual users
  * generates base58 flickr url links to credit respective authors

## examples
1. [Virtual Places](https://play.google.com/store/apps/details?id=org.cmdr2.places)

## development
* get a Google Cardboard. either search on google for cheaper options in your country, or see this list of [viewers in US/EU](https://www.google.com/get/cardboard/get-cardboard/)
* install [Unity 5.x](https://unity3d.com/get-unity/download) (free or pro)
* `git clone git@github.com:cmdr2/panorama.git`
* open the downloaded panoramas project in Unity
* press the play button to run
* `Assets/Scripts/PanelRenderer.cs` contains the core logic

## publishing to play store
* create your [Play Store account](https://play.google.com/apps/publish/)
* open Player settings from Edit > Project Settings > Player
* set the `Bundle Identifier` and `Bundle Version` to your.company.product under 'Other Settings' (e.g. org.cmdr2.places)
* select the `Create New Keystore` checkbox under 'Publishing settings', then press `Browse Keystore` and select user.keystore
* enter a new Keystore password (and confirm password)
* open the `alias` dropdown and select create new. fill in the details, and create a new password
* (optional) click on 'Analytics' in the 'Hierarchy' pane and set the Tracking code, Product Name, and Bundle Identifier to your choice.
* Build the .apk using File > Build Settings > Build.
* Upload the .apk file to Google Play Store and publish after filling in your application details and screenshots.

## license
[MIT](https://github.com/cmdr2/panoramas/blob/master/LICENSE)

## contact
**email:** secondary 'dot' cmdr2 'at' gmail 'dot' com

**reddit:** [u/cmdr2](http://reddit.com/u/cmdr2)

**twitter:** [cmdr2](http://twitter.com/cmdr2)