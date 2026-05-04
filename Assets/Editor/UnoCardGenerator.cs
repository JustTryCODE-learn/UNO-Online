using UnityEngine;
using UnityEditor;

public class UnoCardGenerator : EditorWindow
{
    [MenuItem("Tools/Generate UNO Deck")]
    public static void GenerateDeck()
    {
        string texturePath = "Assets/Textures/Uno_Texture.png";
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath);

        if (sprites == null || sprites.Length <= 1)
        {
            Debug.LogError("No sprites found! Check Sprite Mode Multiple and Apply.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/UnoCards"))
            AssetDatabase.CreateFolder("Assets", "UnoCards");

        int count = 0;
        foreach (Object obj in sprites)
        {
            if (obj is Sprite sprite)
            {
                string n = sprite.name.ToLower();

                // 1. Skip the card back
                if (n.Contains("back")) continue;

                UnoCard card = ScriptableObject.CreateInstance<UnoCard>();
                card.cardSprite = sprite;

                // 2. Fix Color Logic (using lowercase to match n)
                if (n.Contains("red")) card.color = UnoCard.CardColor.Red;
                else if (n.Contains("blue")) card.color = UnoCard.CardColor.Blue;
                else if (n.Contains("green")) card.color = UnoCard.CardColor.Green;
                else if (n.Contains("yellow")) card.color = UnoCard.CardColor.Yellow;
                else if (n.Contains("wild")) card.color = UnoCard.CardColor.Wild;

                // 3. Fix Value Logic
                if (n.Contains("0")) card.value = UnoCard.CardValue.Zero;
                else if (n.Contains("1")) card.value = UnoCard.CardValue.One;
                else if (n.Contains("2")) card.value = UnoCard.CardValue.Two;
                else if (n.Contains("3")) card.value = UnoCard.CardValue.Three;
                else if (n.Contains("4")) card.value = UnoCard.CardValue.Four;
                else if (n.Contains("5")) card.value = UnoCard.CardValue.Five;
                else if (n.Contains("6")) card.value = UnoCard.CardValue.Six;
                else if (n.Contains("7")) card.value = UnoCard.CardValue.Seven;
                else if (n.Contains("8")) card.value = UnoCard.CardValue.Eight;
                else if (n.Contains("9")) card.value = UnoCard.CardValue.Nine;
                else if (n.Contains("+2") || n.Contains("drawtwo")) card.value = UnoCard.CardValue.DrawTwo;
                else if (n.Contains("+4") || n.Contains("drawfour")) card.value = UnoCard.CardValue.DrawFour;
                else if (n.Contains("reverse")) card.value = UnoCard.CardValue.Reverse;
                else if (n.Contains("skip")) card.value = UnoCard.CardValue.Skip;
                else if (n.Contains("wild") && !n.Contains("+4")) card.value = UnoCard.CardValue.Wild;

                AssetDatabase.CreateAsset(card, $"Assets/UnoCards/{sprite.name}.asset");
                count++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully generated {count} cards!");
    }
}