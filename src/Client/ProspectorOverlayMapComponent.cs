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
        private readonly ClientStorage storage;
        private bool hidden = false;
        private Vec2f viewPos = new();

        public ProspectorOverlayMapComponent(ICoreClientAPI clientApi, ChunkCoordinate coords, string message, LoadedTexture colorTexture, ClientStorage storage, bool hidden) : base(clientApi)
        {
            _chunkCoordinates = coords;
            _message = message;
            _chunksize = GlobalConstants.ChunkSize;
            worldPos = new Vec3d(coords.X * _chunksize, 0, coords.Z * _chunksize);
            this.storage = storage;
            this.hidden = hidden;
            this.colorTexture = colorTexture;
        }

        public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            if (IsMouseInsideChunk(args, mapElem))
            {
                hoverText.AppendLine($"\n{_message}");
                hoverText.AppendLine("\n[ProspectTogether] Middle-mouse to hide/show");
            }
        }

        public override void Render(GuiElementMap map, float dt)
        {
            if (hidden)
            {
                return;
            }

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

        public override void OnMouseUpOnElement(MouseEvent args, GuiElementMap mapElem)
        {
            if (args.Handled)
            {
                return;
            }
            if (IsMouseInsideChunk(args, mapElem))
            {
                // Store hidden state
                storage.ToggleHide(_chunkCoordinates);
                // Hide this component
                hidden = !hidden;
                args.Handled = true;
            }
        }

        private bool IsMouseInsideChunk(MouseEvent args, GuiElementMap mapElem)
        { 
            var worldPos = new Vec3d();
            float mouseX = (float)(args.X - mapElem.Bounds.renderX);
            float mouseY = (float)(args.Y - mapElem.Bounds.renderY);

            mapElem.TranslateViewPosToWorldPos(new Vec2f(mouseX, mouseY), ref worldPos);

            var chunkX = (int)(worldPos.X / _chunksize);
            var chunkZ = (int)(worldPos.Z / _chunksize);
            return chunkX == _chunkCoordinates.X && chunkZ == _chunkCoordinates.Z;
        }
    }
}