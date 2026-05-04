using UnityEngine;

[CreateAssetMenu(fileName = "New Uno Card", menuName = "UNO/Card")]
public class UnoCard : ScriptableObject
{
    public enum CardColor { Red, Blue, Green, Yellow, Wild }
    public enum CardValue { Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Skip, Reverse, DrawTwo, Wild, DrawFour }

    [Header("Core Data")]
    public CardColor color;
    public CardValue value;
    public int number; // Used for Zero through Nine
    public Sprite cardSprite;

    [Header("Special Card Flags")]
    public bool isWild; // ONLY ONE DEFINITION HERE NOW
    public bool isSkip;
    public bool isReverse;
    public bool isDrawTwo;

    [Header("Wild Card Specifics")]
    public UnoCard redVersion;
    public UnoCard blueVersion;
    public UnoCard greenVersion;
    public UnoCard yellowVersion;

    public UnoCard GetVersion(CardColor chosenColor)
    {
        switch (chosenColor)
        {
            case CardColor.Red: return redVersion;
            case CardColor.Blue: return blueVersion;
            case CardColor.Green: return greenVersion;
            case CardColor.Yellow: return yellowVersion;
            default: return this;
        }
    }

    // Inside UnoCard class[cite: 13]
    public bool isActionCard 
    {
        get {
            return isSkip || isReverse || isDrawTwo || isWild || value == CardValue.DrawFour;
        }
    }
}