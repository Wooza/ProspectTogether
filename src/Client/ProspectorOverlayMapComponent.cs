using ProspectTogether.Shared;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ProspectTogether.Client
{
    public class ProspectorOverlayMapComponent : MapComponent
    {
        public readonly ChunkCoordinate _chunkCoordinates;

        private readonly string _message;
        private readonly int _chunksize;

        private readonly LoadedTexture colorTexture;
        private readonly Vec3d worldPos = new();
        private Vec2f viewPos = new();

        public ProspectorOverlayMapComponent(ICoreClientAPI clientApi, ChunkCoordinate coords, string message, LoadedTexture colorTexture) : base(clientApi)
        {
            _chunkCoordinates = coords;
            _message = message;
            _chunksize = GlobalConstants.ChunkSize;
            worldPos = new Vec3d(coords.X * _chunksize, 0, coords.Z * _chunksize);
            this.colorTexture = colorTexture;
        }

        public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            var worldPos = new Vec3d();
            float mouseX = (float)(args.X - mapElem.Bounds.renderX);
            float mouseY = (float)(args.Y - mapElem.Bounds.renderY);

            mapElem.TranslateViewPosToWorldPos(new Vec2f(mouseX, mouseY), ref worldPos);

            var chunkX = (int)(worldPos.X / _chunksize);
            var chunkZ = (int)(worldPos.Z / _chunksize);
            if (chunkX == _chunkCoordinates.X && chunkZ == _chunkCoordinates.Z)
            {
                hoverText.AppendLine($"\n{_message}");
            }
        }

        public override void Render(GuiElementMap map, float dt)
        {
            map.TranslateWorldPosToViewPos(worldPos, ref viewPos);
            if (viewPos.X < -2 * _chunksize
                || viewPos.Y < -2 * _chunksize
                || viewPos.X > map.Bounds.OuterWidth + 2 * _chunksize
                || viewPos.Y > map.Bounds.OuterHeight + 2 * _chunksize)
            {
                // Skip rendering if part is not in map bounds.
                return;
            }

            capi.Render.Render2DTexture(
                colorTexture.TextureId,
                (int)(map.Bounds.renderX + viewPos.X),
                (int)(map.Bounds.renderY + viewPos.Y),
                (int)(colorTexture.Width * map.ZoomLevel),
                (int)(colorTexture.Height * map.ZoomLevel),
                50);
        }
    }
}