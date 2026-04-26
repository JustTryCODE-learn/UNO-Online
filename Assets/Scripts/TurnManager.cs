using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public List<string> players = new List<string> { "Player 1", "AI 1", "AI 2", "AI 3" };

    // Assign these in the Unity Inspector by dragging your Player/AI objects here
    public List<HandManager> playerHands;

    public int currentPlayerIndex = 0;
    public bool isClockwise = true;

    void Start()
    {
        if (playerHands == null || playerHands.Count == 0)
        {
            Debug.LogError("Assign your HandManager objects to the TurnManager in the Inspector!");
            return;
        }

        Debug.Log("Game Start! It is " + players[currentPlayerIndex] + "'s turn.");
        StartCoroutine(InitialDeal());
    }

    IEnumerator InitialDeal()
    {
        Debug.Log("Dealing cards...");

        // Deal 7 cards to each player, one by one (for a cool effect)
        for (int i = 0; i < 7; i++)
        {
            foreach (HandManager hand in playerHands)
            {
                hand.DrawCardFromDeck();
                yield return new WaitForSeconds(0.1f); // Short delay between each card
            }
        }

        Debug.Log("Dealing complete!");

        // Check if the FIRST player is an AI and trigger them
        SimpleAI firstAI = playerHands[currentPlayerIndex].GetComponent<SimpleAI>();
        if (firstAI != null)
        {
            firstAI.TakeTurn();
        }
    }

    public int GetNextPlayerIndex()
    {
        int step = isClockwise ? 1 : -1;
        return (currentPlayerIndex + step + players.Count) % players.Count;
    }

    public void HandleDrawPower(int amount)
    {
        int nextPlayer = GetNextPlayerIndex();

        for (int i = 0; i < amount; i++)
        {
            playerHands[nextPlayer].DrawCardFromDeck();
        }

        Debug.Log(players[nextPlayer] + " forced to draw " + amount + " cards!");
        HandleSkip(); // Skip them because they just drew cards
    }

    public void NextTurn()
    {
        int step = isClockwise ? 1 : -1;
        currentPlayerIndex = (currentPlayerIndex + step + players.Count) % players.Count;

        Debug.Log("It is now " + players[currentPlayerIndex] + "'s turn.");

        // Check if the current player has a SimpleAI component
        SimpleAI currentAI = playerHands[currentPlayerIndex].GetComponent<SimpleAI>();

        if (currentAI != null)
        {
            currentAI.TakeTurn();
        }
    }

    public void HandleReverse()
    {
        isClockwise = !isClockwise;
        Debug.Log("Direction Reversed! Clockwise: " + isClockwise);
        NextTurn(); // Move to next person in the new direction
    }

    public void HandleSkip()
    {
        // Jump over the person
        int step = isClockwise ? 1 : -1;
        currentPlayerIndex = (currentPlayerIndex + step + players.Count) % players.Count;

        Debug.Log("Player Skipped!");
        NextTurn(); // Proceed to the person after the skipped one
    }
}