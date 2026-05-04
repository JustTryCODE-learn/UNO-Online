using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public List<string> players = new List<string> { "Player 1", "AI 1", "AI 2", "AI 3" };

    // Assign these in the Unity Inspector by dragging your Player/AI objects here
    public List<HandManager> playerHands;
    public bool isProcessingTurn = false;
    public int currentPlayerIndex = 0;
    public bool isClockwise = true;
    public bool isDealing = true;

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
        isDealing = true;
        Debug.Log("Dealing cards...");

        for (int i = 0; i < 7; i++)
        {
            foreach (HandManager hand in playerHands)
            {
                hand.DrawCardFromDeck();
                yield return new WaitForSeconds(0.1f);
            }
        }

        Debug.Log("Dealing complete! Waiting for Player 1.");
        currentPlayerIndex = 0;
        isDealing = false; // Now turns can finally start
    }

    public void NextTurn()
    {
        // Prevent overlapping turn calls
        if (isDealing || isProcessingTurn) return; 
        
        StartCoroutine(NextTurnRoutine());
    }

    IEnumerator NextTurnRoutine()
    {
        isProcessingTurn = true;

        // Small delay so the player can see the card that was just played
        yield return new WaitForSeconds(1.0f);

        int step = isClockwise ? 1 : -1;
        currentPlayerIndex = (currentPlayerIndex + step + players.Count) % players.Count;

        Debug.Log("It is now " + players[currentPlayerIndex] + "'s turn.");

        SimpleAI currentAI = playerHands[currentPlayerIndex].GetComponent<SimpleAI>();
        
        if (currentAI != null)
        {
            currentAI.TakeTurn();
        }

        isProcessingTurn = false;
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

    public void PlayerRequestsDraw()
    {
        if (currentPlayerIndex == 0 && !isDealing && !isProcessingTurn) 
        {
            playerHands[0].DrawCardFromDeck();
            NextTurn(); // This now uses the delayed Routine
        }
    }
}