using UnityEngine;

public class GameHUD : MonoBehaviour
{
    private PlayerMovement player;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle boxStyle;
    private Texture2D bgTexture;

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerMovement>();
        }
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.85f));
            bgTexture.Apply();

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = bgTexture;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            headerStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal
            };
            subHeaderStyle.normal.textColor = Color.white;
        }
    }

    private void OnGUI()
    {
        InitStyles();

        // Top Left: Health & Controls
        GUILayout.BeginArea(new Rect(15, 15, 420, 160), boxStyle);
        GUILayout.Space(6);

        if (player != null)
        {
            string hearts = "";
            for (int i = 0; i < player.MaxHealth; i++)
            {
                hearts += (i < player.CurrentHealth) ? " <color=#ff3344>♥</color>" : " <color=#666666>♡</color>";
            }

            if (player.IsDead)
            {
                GUILayout.Label($"<b>PLAYER STATUS:</b> <color=red>GAME OVER</color>", headerStyle);
            }
            else
            {
                GUILayout.Label($"<b>PLAYER HEALTH:</b>{hearts} ({player.CurrentHealth}/{player.MaxHealth})", headerStyle);
            }
        }
        else
        {
            GUILayout.Label("<b>THE BOMB GAME</b>", headerStyle);
        }

        GUILayout.Space(4);
        GUILayout.Label("<b>CONTROLS:</b>", subHeaderStyle);
        GUILayout.Label("• <b>Move:</b> A / D or Arrow Keys", subHeaderStyle);
        GUILayout.Label("• <b>Jump:</b> W / Space Bar / Up Arrow", subHeaderStyle);
        GUILayout.Label("• <b>Throw Bomb:</b> F / E / Left Mouse Click", subHeaderStyle);
        GUILayout.EndArea();

        // Top Right: Enemy Intel
        GUILayout.BeginArea(new Rect(Screen.width - 340, 15, 325, 160), boxStyle);
        GUILayout.Space(6);
        GUILayout.Label("<b>ENEMY INTEL:</b>", headerStyle);
        GUILayout.Label("• <color=#ffd24d>Bald Pirate</color>: Kicks bomb to dodge (1 bomb)", subHeaderStyle);
        GUILayout.Label("• <color=#ff9966>Big Guy</color>: Catches & throws bomb at you (3 bombs)", subHeaderStyle);
        GUILayout.Label("• <color=#85e085>Cucumber</color>: Bites player (2 bombs)", subHeaderStyle);
        GUILayout.Label("• <color=#66ccff>Captain</color>: Fearful runner (1 bomb)", subHeaderStyle);
        GUILayout.Label("• <color=#b366ff>Whale</color>: Swallows bombs & bites (Lethal fall damage!)", subHeaderStyle);
        GUILayout.EndArea();
    }
}
