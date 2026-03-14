using UnityEngine;

public class PlayerHUD : WorldBehaviour {

    private Texture2D marqueeBoxTexture;
    private Texture2D unitHealthBarTexture_Green;
    private Texture2D unitHealthBarTexture_Yellow;
    private Texture2D unitHealthBarTexture_Red;

    private GUIStyle marqueeBoxStyle;

    private GUIStyle unitHealthBarStyle_Background;
    private GUIStyle unitHealthBarStyle_Green;
    private GUIStyle unitHealthBarStyle_Yellow;
    private GUIStyle unitHealthBarStyle_Red;

    public float unitHealthBarHeight = 2.5f;
    public Vector2 unitHealthBarPadding = new(1, 1);

    public Rect GetOnScreenBounds(Renderer renderer, Camera camera) {
        var bounds = renderer.bounds;
        var corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = bounds.max;
        corners[2] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var corner in corners) {
            var screenPoint = camera.WorldToScreenPoint(corner);
            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static Rect ToGUICoordinates(Rect rect) {
        return new Rect(rect.x, Screen.height - rect.yMax, rect.width, rect.height);
    }

    public void DrawRectangle(Rect rectangle, GUIStyle style = null) {
        GUI.Box(rectangle, "", style ?? GUI.skin.box);
    }

    public void OnGUI() {
        if (!marqueeBoxTexture) {
            marqueeBoxTexture = new Texture2D(1, 1);
            marqueeBoxTexture.SetPixel(0, 0, new Color(1, 1, 1, .5f));
            marqueeBoxTexture.Apply();
        }
        if (!unitHealthBarTexture_Green) {
            unitHealthBarTexture_Green = new Texture2D(1, 1);
            unitHealthBarTexture_Green.SetPixel(0, 0, new Color(0, 1, 0, 1));
            unitHealthBarTexture_Green.Apply();
        }
        if (!unitHealthBarTexture_Yellow) {
            unitHealthBarTexture_Yellow = new Texture2D(1, 1);
            unitHealthBarTexture_Yellow.SetPixel(0, 0, new Color(1, 1, 0, 1));
            unitHealthBarTexture_Yellow.Apply();
        }
        if (!unitHealthBarTexture_Red) {
            unitHealthBarTexture_Red = new Texture2D(1, 1);
            unitHealthBarTexture_Red.SetPixel(0, 0, new Color(1, 0, 0, 1));
            unitHealthBarTexture_Red.Apply();
        }
        marqueeBoxStyle ??= new GUIStyle {
            normal = {
                background = marqueeBoxTexture
            }
        };
        unitHealthBarStyle_Background ??= new GUIStyle {
            normal = {
                background = Texture2D.whiteTexture
            }
        };
        unitHealthBarStyle_Green ??= new GUIStyle {
            normal = {
                background = unitHealthBarTexture_Green
            }
        };
        unitHealthBarStyle_Yellow ??= new GUIStyle {
            normal = {
                background = unitHealthBarTexture_Yellow
            }
        };
        unitHealthBarStyle_Red ??= new GUIStyle {
            normal = {
                background = unitHealthBarTexture_Red
            }
        };

        if (world.playerController.marqueeStart is { } actualMarqueeStart) {
            var min = Vector2.Min(actualMarqueeStart, world.playerController.marqueeEnd);
            var max = Vector2.Max(actualMarqueeStart, world.playerController.marqueeEnd);
            var rectangle = new Rect(min, max - min);
            DrawRectangle(ToGUICoordinates(rectangle), marqueeBoxStyle);
        }

        var unitsRegistry = world.GetSubsystem<UnitsRegistry>();
        if (unitsRegistry)
            foreach (var unit in unitsRegistry.units) {
                var isSelected = world.playerController.selectedUnits.Contains(unit);
                if (isSelected) {
                    var onScreenBounds = GetOnScreenBounds(unit.meshRenderer, world.playerController.playerCamera);
                    var healthBarRectangle = ToGUICoordinates(new Rect(
                        onScreenBounds.xMin, onScreenBounds.yMin - unitHealthBarHeight,
                        onScreenBounds.width, unitHealthBarHeight
                    ));

                    var healthBarBackgroundRectangle = healthBarRectangle;
                    healthBarBackgroundRectangle.x -= unitHealthBarPadding.x;
                    healthBarBackgroundRectangle.width += unitHealthBarPadding.x * 2;
                    healthBarBackgroundRectangle.y -= unitHealthBarPadding.y;
                    healthBarBackgroundRectangle.height += unitHealthBarPadding.y * 2;
                    DrawRectangle(healthBarBackgroundRectangle, unitHealthBarStyle_Background);

                    var healthBarFilledRectangle = healthBarRectangle;
                    healthBarFilledRectangle.width = healthBarRectangle.width * unit.health;
                    var fillStyle = unit.health switch {
                        > .5f => unitHealthBarStyle_Green,
                        > .25f => unitHealthBarStyle_Yellow,
                        _ => unitHealthBarStyle_Red
                    };
                    DrawRectangle(healthBarFilledRectangle, fillStyle);
                }
            }
    }
}