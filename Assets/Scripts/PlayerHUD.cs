using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHUD : WorldBehaviour {

    [Serializable]
    private struct HealthColorRampPoint {
        [SerializeField] public float end;
        [SerializeField] public Color color;
        [NonSerialized] public Texture2D onePixelTexture;
        [NonSerialized] public GUIStyle style;
    }

    public PlayerController owningPlayerController;

    [Header("Selection Marquee")] [SerializeField]
    private Color marqueeColor = new(1, 1, 1, .5f);

    private Texture2D marqueeBackgroundTexture;
    private GUIStyle marqueeGUIStyle;

    [Header("Unit Health Bar")] [SerializeField]
    private Color unitHealthBarBackgroundColor = new(0, 0, 0, .5f);

    private Texture2D unitHealthBarBackgroundTexture;
    private GUIStyle unitHealthBarGUIStyle;

    [SerializeField] private float unitHealthBarHeight = 2.5f;
    [SerializeField] private Vector2 unitHealthBarPadding = new(1, 1);

    [SerializeField] private List<HealthColorRampPoint> unitHealthBarColorRamp = new() {
        new HealthColorRampPoint { end = .25f, color = Color.red },
        new HealthColorRampPoint { end = .5f, color = Color.yellow },
        new HealthColorRampPoint { end = 1, color = Color.green },
    };

    [SerializeField] private RadarRenderer radarRenderer;

    private void Awake() {
        marqueeBackgroundTexture = new Texture2D(1, 1);
        marqueeBackgroundTexture.SetPixel(0, 0, marqueeColor);
        marqueeBackgroundTexture.Apply();
        marqueeGUIStyle = new GUIStyle {
            normal = {
                background = marqueeBackgroundTexture
            }
        };

        for (var i = 0; i < unitHealthBarColorRamp.Count; i++) {
            var rampPoint = unitHealthBarColorRamp[i];
            rampPoint.onePixelTexture = new Texture2D(1, 1);
            rampPoint.onePixelTexture.SetPixel(0, 0, rampPoint.color);
            rampPoint.onePixelTexture.Apply();
            rampPoint.style = new GUIStyle {
                normal = {
                    background = rampPoint.onePixelTexture
                }
            };
            unitHealthBarColorRamp[i] = rampPoint;
        }

        unitHealthBarBackgroundTexture = new Texture2D(1, 1);
        unitHealthBarBackgroundTexture.SetPixel(0, 0, unitHealthBarBackgroundColor);
        unitHealthBarBackgroundTexture.Apply();
        unitHealthBarGUIStyle = new GUIStyle {
            normal = {
                background = unitHealthBarBackgroundTexture
            }
        };
        
        radarRenderer.world = world;
    }

    public Rect GetOnScreenBounds(Bounds bounds, Camera camera) {
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

    private void OnGUI() {
        
        if (owningPlayerController.marqueeStart is { } actualMarqueeStart) {
            var min = Vector2.Min(actualMarqueeStart, owningPlayerController.marqueeEnd);
            var max = Vector2.Max(actualMarqueeStart, owningPlayerController.marqueeEnd);
            var rectangle = new Rect(min, max - min);
            DrawRectangle(ToGUICoordinates(rectangle), marqueeGUIStyle);
        }

        var selectablesRegistry = world.GetSubsystem<SelectablesRegistry>();
        if (selectablesRegistry)
            foreach (var selectable in selectablesRegistry.Entities)
                if (selectable.IsSelected && selectable is IHasHealth selectableHealth) {
                    var onScreenBounds = GetOnScreenBounds(selectable.SelectionBounds, owningPlayerController.PlayerCamera);
                    var healthBarRectangle = ToGUICoordinates(new Rect(
                        onScreenBounds.xMin, onScreenBounds.yMin - unitHealthBarHeight,
                        onScreenBounds.width, unitHealthBarHeight
                    ));

                    var healthBarBackgroundRectangle = healthBarRectangle;
                    healthBarBackgroundRectangle.x -= unitHealthBarPadding.x;
                    healthBarBackgroundRectangle.width += unitHealthBarPadding.x * 2;
                    healthBarBackgroundRectangle.y -= unitHealthBarPadding.y;
                    healthBarBackgroundRectangle.height += unitHealthBarPadding.y * 2;
                    DrawRectangle(healthBarBackgroundRectangle, unitHealthBarGUIStyle);

                    var healthBarFilledRectangle = healthBarRectangle;
                    healthBarFilledRectangle.width = healthBarRectangle.width * selectableHealth.Health;

                    var intervalStart = .0f;
                    GUIStyle fillStyle = null;
                    for (var i = 0; i < unitHealthBarColorRamp.Count; i++) {
                        var rampPoint = unitHealthBarColorRamp[i];
                        var intervalEnd = rampPoint.end;
                        if (selectableHealth.Health >= intervalStart && selectableHealth.Health <= intervalEnd) {
                            fillStyle = rampPoint.style;
                            break;
                        }
                        intervalStart = intervalEnd;
                    }
                    if (fillStyle != null)
                        DrawRectangle(healthBarFilledRectangle, fillStyle);
                }
    }
}