<h1 align="center">
  <br>
  🏥 VR Healthy
  <br>
</h1>

<h4 align="center">A Virtual Reality educational and exploration experience built with Unity.</h4>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#controls">Controls</a> •
  <a href="#scripts-overview">Scripts Overview</a> •
  <a href="#getting-started">Getting Started</a>
</p>

![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-black?style=for-the-badge&logo=unity)
![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20Cardboard-green?style=for-the-badge&logo=android)
![Language](https://img.shields.io/badge/Language-C%23-blue?style=for-the-badge&logo=c-sharp)

## 📖 Description

**VR Healthy** is an interactive Virtual Reality application focused on health education and spatial exploration. Players can navigate through virtual environments (like a clinic or hospital room), interact with educational posters, open doors, and learn through a highly immersive experience designed for Google Cardboard and mobile VR headsets.

## ✨ Features

- **Immersive Locomotion:** Look-based walking system (`VRWalkController`) that allows movement without complex joystick inputs.
- **Gaze-based Interactions:** Look at objects to interact with them, ensuring a seamless VR experience.
- **Dynamic UI Panels:** Educational posters feature premium smooth fade in/out UI transitions when interacted with.
- **Interactive Environment:** Functional doors that can be opened/closed via gaze and button press.
- **Mobile VR Ready:** Optimized for Android and Google Cardboard setups.

## 🎮 Controls

The application uses a hybrid of gaze (looking) and simple button inputs, perfect for basic VR headsets.

| Action | Input | Description |
| :--- | :--- | :--- |
| **Move Forward** | **Look Up** | Tilt your head up past the threshold to start walking forward automatically. |
| **Interact / Open** | **`E` or Trigger** | Press when near an interactive object (like a Door or Poster) to open it. |
| **Close UI** | **`Q`** | Close an active poster panel or UI element. |
| **Look Around** | **Head Tracking** | Move your head to look around the environment. |

## 🛠️ Scripts Overview

Here are the core mechanics driving the VR experience:

*   **`InteractionPoster.cs`**: Handles player proximity and gaze detection for educational posters. It features a premium, code-driven smooth fade transition using `CanvasGroup` to display informational UI panels.
*   **`VRWalkController.cs`**: A custom CharacterController script that moves the player forward based on the camera's pitch angle (looking up).
*   **`DoorMechanic.cs`**: Manages the logic for interactive doors, allowing them to swing open and closed smoothly when the player focuses on them and interacts.

## 🚀 Getting Started

### Prerequisites
*   **Unity 2022.3 LTS** (or newer)
*   **Android Build Support** installed in Unity Hub
*   **Google Cardboard XR Plugin** for Unity

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/edwardsajaaa/VR_Healty.git
   ```
2. Open Unity Hub and add the cloned folder.
3. Open the project. Make sure your Build Settings are set to **Android**.
4. Open the `Gameplay` scene located in `Assets/Scenes/`.
5. Press **Play** in the editor to test, or click **Build and Run** to deploy to your Android device.

---

<p align="center">
  Built with ❤️ for Health Education in VR.
</p>