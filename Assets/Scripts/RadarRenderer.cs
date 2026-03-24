using UnityEngine;
using UnityEngine.UI;

public class RadarRenderer : WorldBehaviour {

    [SerializeField] private Vector2Int size = new(256, 256);
    [SerializeField] private RenderTexture texture;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private Material material;

    public RenderTexture Texture => texture;

    private void Awake() {
        texture = new RenderTexture(size.x, size.y, 0);
        texture.Create();

        targetImage.texture = texture;
    }

    private void OnDestroy() {
        if (texture)
            texture.Release();
    }

    public void RenderRadar() {
        
        // all of this does not work currently
        
        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = texture;

        GL.Clear(true, true, Color.black);
        
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, size.x, 0, size.y);
        
        material.SetPass(0);

        var lowerLeft = world.worldBounds.bounds.min.ToVector2();
        var topRight = world.worldBounds.bounds.max.ToVector2();
        
        var unitsRegistry = world.GetSubsystem<UnitsRegistry>();
        if (unitsRegistry) {
            foreach (var unit in unitsRegistry.Entities) {
                var unitPosition2D = unit.transform.position.ToVector2();
                var normalizedPosition = (unitPosition2D - lowerLeft) / (topRight - lowerLeft);
                var pixelPosition = new Vector2(normalizedPosition.x * size.x, normalizedPosition.y * size.y);
                var rectangle = Rect.MinMaxRect(pixelPosition.x - 10, pixelPosition.y - 10, pixelPosition.x + 10, pixelPosition.y + 10);
                
                var pos = unit.transform.position.ToVector2();
                var norm = (pos - lowerLeft) /  (topRight - lowerLeft);
                
                var px = norm.x * size.x;
                var py = norm.y * size.y;

                float half = 10f;
                
                GL.Begin(GL.QUADS);
                GL.Color(unit.OwningPlayer.Color);

                GL.Vertex3(px - half, py - half, 0);
                GL.Vertex3(px + half, py - half, 0);
                GL.Vertex3(px + half, py + half, 0);
                GL.Vertex3(px - half, py + half, 0);

                GL.End();
            }
        }
        
        GL.PopMatrix();

        RenderTexture.active = previousRenderTexture;
    }

    public void Update() {
        RenderRadar();
    }
}