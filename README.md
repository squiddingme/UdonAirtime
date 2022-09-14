# UdonAirtime
An expressive movement system for VRChat. Includes variable jump height, double jumping, wall riding, wall jumping, and rail grinding via an Udon-powered bézier curve implementation.

## Requirements
* Unity 2019.4.x
* VRCSDK3 (Minimum WORLD-2021.08.04.15.07)
* [UdonSharp](https://github.com/MerlinVR/UdonSharp) (Minimum 0.20.3)
* (Optional) [CyanPlayerObjectPool](https://github.com/CyanLaser/CyanPlayerObjectPool)

## Download
Get it in the [releases](https://github.com/squiddingme/UdonAirtime/releases).

## Basic Setup
1. Import VRChat SDK3 to project (or use the [Creator Companion](https://vrchat.com/home/download))
2. Don't forget to let the SDK set up your layers and collision matrix
3. Import [UdonSharp](https://github.com/MerlinVR/UdonSharp) to project (or use the [Creator Companion](https://vrchat.com/home/download))
4. (Optional) Import [CyanPlayerObjectPool](https://github.com/CyanLaser/CyanPlayerObjectPool/releases) to project to use the PooledPlayerController, which allows everyone to see each other's effects and sounds
5. Import UdonAirtime package to project

Follow additional instructions and documentation found in readme.pdf in release packages for world and grind rail setup.

## License
This work is licensed under the terms of the MIT license.