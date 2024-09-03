using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using STROOP.Utilities;
using STROOP.Structs.Configurations;
using STROOP.Structs;
using OpenTK;
using System.Xml.Linq;
using System.Windows.Forms;

namespace STROOP.Map
{
    public class MapObjectPyramidPlatformNormals : MapObject
    {
        private readonly PositionAngle _posAngle;

        private float? CustomNormalX;
        private float? CustomNormalY;
        private float? CustomNormalZ;

        private bool UsingCustom => CustomNormalX.HasValue || CustomNormalY.HasValue || CustomNormalZ.HasValue;

        public MapObjectPyramidPlatformNormals(PositionAngle posAngle)
            : base()
        {
            _posAngle = posAngle;

            CustomNormalX = null;
            CustomNormalY = null;
            CustomNormalZ = null;

            Opacity = 0.5;
        }

        public override void DrawOn2DControlTopDownView(MapObjectHoverData hoverData)
        {
            uint objAddress = _posAngle.GetObjAddress();

            DrawCircles(Color.Purple);
            DrawHyperbolas(true, CustomNormalX, Config.Stream.GetFloat(objAddress + ObjectConfig.PyramidPlatformNormalXOffset), Color.DarkRed);
            DrawHyperbolas(false, CustomNormalZ, Config.Stream.GetFloat(objAddress + ObjectConfig.PyramidPlatformNormalZOffset), Color.Lime);
        }

        private float ApproachNormal(float? startNullable, float end)
        {
            if (!startNullable.HasValue)
            {
                return end;
            }

            float start = startNullable.Value;

            while (start + 0.01f <= end)
            {
                start += 0.01f;
            }

            while (start - 0.01f >= end)
            {
                start -= 0.01f;
            }

            return start;
        }

        private void DrawCircles(Color color)
        {
            uint objAddress = _posAngle.GetObjAddress();
            float normalY = CustomNormalY ?? Config.Stream.GetFloat(objAddress + ObjectConfig.PyramidPlatformNormalYOffset);
            float approachedNormal = ApproachNormal(CustomNormalY, Config.Stream.GetFloat(objAddress + ObjectConfig.PyramidPlatformNormalYOffset));

            double r1 = 500 * Math.Sqrt(1 / ((normalY + 0.01) * (normalY + 0.01)) - 1);
            double r2 = 500 * Math.Sqrt(1 / ((normalY) * (normalY)) - 1);
            double r3 = 500 * Math.Sqrt(1 / ((normalY - 0.01) * (normalY - 0.01)) - 1);
            double r4 = 500 * Math.Sqrt(1 / ((approachedNormal) * (approachedNormal)) - 1);
            double r5 = 500 * Math.Sqrt(1 / ((CustomNormalY ?? normalY) * (CustomNormalY ?? normalY)) - 1);

            if (UsingCustom)
            {
                DrawCircle((float)_posAngle.X, (float)_posAngle.Z, (float)r4, color);
                DrawCircle((float)_posAngle.X, (float)_posAngle.Z, (float)r5, color);
            }
            else
            {
                ShadeBetweenCircles((float)_posAngle.X, (float)_posAngle.Z, (float)r1, (float)r2, color.Lighten(0.5));
                ShadeBetweenCircles((float)_posAngle.X, (float)_posAngle.Z, (float)r2, (float)r3, color.Lighten(0.5));

                DrawCircle((float)_posAngle.X, (float)_posAngle.Z, (float)r1, color);
                DrawCircle((float)_posAngle.X, (float)_posAngle.Z, (float)r2, color);
                DrawCircle((float)_posAngle.X, (float)_posAngle.Z, (float)r3, color);
            }
        }

        private void DrawCircle(float centerX, float centerZ, float radius, Color color)
        {
            (float controlCenterX, float controlCenterZ) = MapUtilities.ConvertCoordsForControlTopDownView(centerX, centerZ, UseRelativeCoordinates);
            float controlRadius = radius * Config.CurrentMapGraphics.MapViewScaleValue;
            List<(float pointX, float pointZ)> controlPoints = Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 65536)
                .ConvertAll(angle => ((float, float))MoreMath.AddVectorToPoint(controlRadius, angle, controlCenterX, controlCenterZ));

            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Draw outline
            if (LineWidth != 0)
            {
                GL.Color4(color.R, color.G, color.B, (byte)255);
                GL.LineWidth(LineWidth);
                GL.Begin(PrimitiveType.LineLoop);
                foreach ((float x, float z) in controlPoints)
                {
                    GL.Vertex2(x, z);
                }
                GL.End();
            }

            GL.Color4(1, 1, 1, 1.0f);
        }

        private void ShadeBetweenCircles(float centerX, float centerZ, float radius1, float radius2, Color color)
        {
            (float controlCenterX, float controlCenterZ) = MapUtilities.ConvertCoordsForControlTopDownView(centerX, centerZ, UseRelativeCoordinates);
            float controlRadius1 = radius1 * Config.CurrentMapGraphics.MapViewScaleValue;
            float controlRadius2 = radius2 * Config.CurrentMapGraphics.MapViewScaleValue;
            List<(float pointX, float pointZ)> controlPoints1 = Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 65536)
                .ConvertAll(angle => ((float, float))MoreMath.AddVectorToPoint(controlRadius1, angle, controlCenterX, controlCenterZ));
            List<(float pointX, float pointZ)> controlPoints2 = Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 65536)
                .ConvertAll(angle => ((float, float))MoreMath.AddVectorToPoint(controlRadius2, angle, controlCenterX, controlCenterZ));

            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Draw circle
            GL.Color4(color.R, color.G, color.B, OpacityByte);
            GL.Begin(PrimitiveType.QuadStrip);
            for (int i = 0; i <= controlPoints1.Count; i++)
            {
                int index = i % controlPoints1.Count;
                GL.Vertex2(controlPoints1[index].pointX, controlPoints1[index].pointZ);
                GL.Vertex2(controlPoints2[index].pointX, controlPoints2[index].pointZ);
            }
            GL.End();

            GL.Color4(1, 1, 1, 1.0f);
        }

        private void DrawHyperbolas(bool isForX, float? customNormal, float inGameNormal, Color color)
        {
            List<double> offsets = new List<double>() { -0.01, 0, 0.01 };
            List<double> offsetedNormals = offsets.ConvertAll(offset => inGameNormal + offset);

            float approachedNormal = ApproachNormal(customNormal, inGameNormal);
            offsetedNormals.Add(approachedNormal);
            offsetedNormals.Add(customNormal ?? inGameNormal);

            double range = 1000;
            List<List<(float pointX, float pointZ)>> pointLists =
                offsetedNormals.ConvertAll(offsetedNormal =>
                {
                    if (isForX)
                    {
                        return Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                            .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 2 * range - range + _posAngle.Z)
                            .ConvertAll(z => (Math.Sign(offsetedNormal) * Math.Sqrt((250000 + ((z - _posAngle.Z) * (z - _posAngle.Z))) / ((1 / ((offsetedNormal) * (offsetedNormal))) - 1)) + _posAngle.X, z))
                            .ConvertAll(p => MapUtilities.ConvertCoordsForControlTopDownView((float)p.Item1, (float)p.z, UseRelativeCoordinates));
                    }
                    else
                    {
                        return Enumerable.Range(0, MapConfig.MapCircleNumPoints2D).ToList()
                            .ConvertAll(index => (index / (float)MapConfig.MapCircleNumPoints2D) * 2 * range - range + _posAngle.X)
                            .ConvertAll(x => (Math.Sign(offsetedNormal) * Math.Sqrt((250000 + ((x - _posAngle.X) * (x - _posAngle.X))) / ((1 / ((offsetedNormal) * (offsetedNormal))) - 1)) + _posAngle.Z, x))
                            .ConvertAll(p => MapUtilities.ConvertCoordsForControlTopDownView((float)p.x, (float)p.Item1, UseRelativeCoordinates));
                    }
                });

            if (UsingCustom)
            {
                DrawHyperbola(pointLists[3], color);
                DrawHyperbola(pointLists[4], color);
            }
            else
            {
                ShadeBetweenHyperbolas(pointLists[0], pointLists[1], color.Lighten(0.5));
                ShadeBetweenHyperbolas(pointLists[1], pointLists[2], color.Lighten(0.5));

                DrawHyperbola(pointLists[0], color);
                DrawHyperbola(pointLists[1], color);
                DrawHyperbola(pointLists[2], color);
            }
        }

        private void DrawHyperbola(List<(float pointX, float pointZ)> controlPoints, Color color)
        {
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Draw outline
            if (LineWidth != 0)
            {
                GL.Color4(color.R, color.G, color.B, (byte)255);
                GL.LineWidth(LineWidth);
                GL.Begin(PrimitiveType.LineStrip);
                foreach ((float x, float z) in controlPoints)
                {
                    GL.Vertex2(x, z);
                }
                GL.End();
            }

            GL.Color4(1, 1, 1, 1.0f);
        }

        private void ShadeBetweenHyperbolas(
            List<(float pointX, float pointZ)> controlPoints1, List<(float pointX, float pointZ)> controlPoints2, Color color)
        {
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Draw circle
            GL.Color4(color.R, color.G, color.B, OpacityByte);
            GL.Begin(PrimitiveType.QuadStrip);
            for (int i = 0; i < controlPoints1.Count; i++)
            {
                GL.Vertex2(controlPoints1[i].pointX, controlPoints1[i].pointZ);
                GL.Vertex2(controlPoints2[i].pointX, controlPoints2[i].pointZ);
            }
            GL.End();

            GL.Color4(1, 1, 1, 1.0f);
        }

        public override void DrawOn2DControlOrthographicView(MapObjectHoverData hoverData)
        {
            // do nothing
        }

        public override void DrawOn3DControl()
        {
            // do nothing
        }

        public override MapDrawType GetDrawType()
        {
            return MapDrawType.Perspective;
        }

        public override string GetName()
        {
            string prefix = "";
            if (CustomNormalX.HasValue && CustomNormalY.HasValue && CustomNormalZ.HasValue)
            {
                prefix = $"{CustomNormalX.Value}, {CustomNormalY.Value}, {CustomNormalZ.Value} ";
            }
            return prefix + "Pyramid Platform Normals for " + _posAngle.GetMapName();
        }

        public override Image GetInternalImage()
        {
            return Config.ObjectAssociations.CoffinBoxImage;
        }

        public override PositionAngle GetPositionAngle()
        {
            return _posAngle;
        }

        public override ContextMenuStrip GetContextMenuStrip()
        {
            if (_contextMenuStrip == null)
            {
                ToolStripMenuItem itemSetNormal = new ToolStripMenuItem("Set Normal");
                itemSetNormal.Click += (sender, e) =>
                {
                    string text = DialogUtilities.GetStringFromDialog(labelText: "Enter nx, ny, and nz:");
                    List<double?> values = ParsingUtilities.ParseDoubleList(text);
                    if (values.Count < 3 || !values[0].HasValue || !values[1].HasValue || !values[2].HasValue)
                    {
                        return;
                    }
                    float nx = (float)values[0].Value;
                    float ny = (float)values[1].Value;
                    float nz = (float)values[2].Value;
                    MapObjectSettings settings = new MapObjectSettings(
                        changeNormalX: true, changeNormalY: true, changeNormalZ: true, newNormalX: nx, newNormalY: ny, newNormalZ: nz);
                    GetParentMapTracker().ApplySettings(settings);
                };

                ToolStripMenuItem itemClearNormal = new ToolStripMenuItem("Clear Normal");
                itemClearNormal.Click += (sender, e) =>
                {
                    MapObjectSettings settings = new MapObjectSettings(
                        changeNormalX: true, changeNormalY: true, changeNormalZ: true, newNormalX: null, newNormalY: null, newNormalZ: null);
                    GetParentMapTracker().ApplySettings(settings);
                };

                _contextMenuStrip = new ContextMenuStrip();
                _contextMenuStrip.Items.Add(itemSetNormal);
                _contextMenuStrip.Items.Add(itemClearNormal);
            }

            return _contextMenuStrip;
        }

        public override void ApplySettings(MapObjectSettings settings)
        {
            base.ApplySettings(settings);

            if (settings.ChangeNormalX)
            {
                CustomNormalX = settings.NewNormalX;
            }
            if (settings.ChangeNormalY)
            {
                CustomNormalY = settings.NewNormalY;
            }
            if (settings.ChangeNormalZ)
            {
                CustomNormalZ = settings.NewNormalZ;
            }
        }


        public override List<XAttribute> GetXAttributes()
        {
            return new List<XAttribute>()
            {
                new XAttribute("positionAngle", _posAngle),
            };
        }
    }
}
