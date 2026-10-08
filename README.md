# Intelligent Virtual Agent for XR Study Briefing and Consent

This Unity project explores the use of an **Intelligent Virtual Agent (IVA)** to provide participant briefing and consent information before a VR experiment. The IVA explains the study procedure, and allows participants to ask follow-up questions.

The project was developed as part of an MSc research project and uses a simple **Fitts' Law pointing task** as the experimental scenario.

## Requirements

* Unity
* Meta Quest headset
* Meta XR SDK
* UHH Intelligent Virtual Agent SDK
* Google Gemini API access

## Setup

1. Clone or download the project.
2. Open the project in Unity.
3. Make sure the required Meta XR and IVA SDK packages are installed.
4. Configure the Gemini API credentials required by the IVA SDK.
5. Connect the Meta Quest using Quest Link.
6. Open the main experiment scene and enter Play Mode.

## How to Use

The experiment follows this general flow:

`Welcome → Briefing → Consent → Practice → Experiment → Finished`

* **Start Briefing** – starts the IVA-led study briefing.
* **Continue** – proceeds to the consent stage.
* **Ask Questions** – participants can verbally ask the IVA for clarification during the briefing.
* **Consent** – participants can decide whether to continue with the study.
* **Practice** – introduces the VR pointing interaction.
* **Start Experiment** – launches the simplified Fitts' Law pointing task.

![image](https://github.com/kx0385/XR_IVA/blob/main/Assets/Materials/example.png)

The IVA provides spoken explanations and answers participant questions, while Unity controls the experiment stages and interface.
