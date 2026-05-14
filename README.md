# 🎴 Photon Unity UNO (Multiplayer)

A real-time, cross-platform multiplayer UNO game built with **Unity** and **Photon PUN 2**. This project features standard UNO gameplay alongside custom "House Rules" for a more dynamic experience.

---

## 🚀 Key Features

* **Real-time Multiplayer:** Seamless networking powered by Photon PUN 2.
* **Custom House Rules:**
    * **The 7 Rule (Swap):** Play a 7 to pick an opponent and trade your entire hand with theirs.
    * **The 0 Rule (Rotate):** Play a 0 and choose a direction (Clockwise/Counter) to rotate all players' hands.
* **Stacking System:** Chain +2 and +4 cards to pass massive penalties to the next player.
* **UNO Shout Mechanic:** A high-stakes reaction window—click "UNO" before time runs out or face a 2-card penalty.
* **Interactive UI:** Dynamic hand layouts, color pickers, and a paginated rule system.

---

## 🛠 Installation & Setup

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/YourUsername/YourRepoName.git
    ```
2.  **Open in Unity:** Use Unity version **2021.3 LTS** or newer.
3.  **Photon Setup:**
    * Create a free account at [Photon Engine](https://dashboard.photonengine.com/).
    * Create a new **PUN App ID**.
    * In Unity, go to `Window > Photon Unity Networking > Highlight Server Settings`.
    * Paste your **App Id** into the Inspector.
4.  **Network Sync:** Ensure all players are on the same **Fixed Region** (e.g., `asia` or `us`) in `PhotonServerSettings` to see the same rooms.

---

## 🎮 How to Play

### Basic Rules
* **Match** cards by color or number.
* If you have no legal moves, you must **Draw** from the deck.
* When down to your second-to-last card, you must click the **UNO button** before the timer ends.

### Special Cards
| Card | Effect |
| :--- | :--- |
| **Skip** | The next player loses their turn. |
| **Reverse** | Flips the direction of play. |
| **Draw 2** | Next player draws 2 and is skipped (unless they stack). |
| **Wild** | Choose the active color for the next turn. |

---

## 📂 Project Structure

* **Assets/Scripts/OnlineGame:** Core multiplayer logic and Master Client authority scripts.
* **Assets/Scripts/UI:** Paginated rule system and dynamic UI updates.
* **Assets/Prefabs:** Synced card objects and UI panels.

---

## 🤝 Contributors
* Developed by **[Your Name/Team Name]**
* Built with Unity & Photon PUN 2.
