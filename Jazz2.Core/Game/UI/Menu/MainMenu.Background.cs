﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Duality;
using Duality.Drawing;
using Duality.IO;
using Duality.Resources;
using Jazz2.Game.Structs;
using Jazz2.Game.Tiles;

namespace Jazz2.Game.UI.Menu
{
    partial class MainMenu
    {
        private ContentRef<Texture> cachedTexturedBackground;
        private ContentRef<DrawTechnique> texturedBackgroundShader;
        private VertexC1P3T2[] cachedVertices;
        private float backgroundX, backgroundY, backgroundPhase;
        private Vector4 horizonColor;

        private void PrerenderTexturedBackground()
        {
            try {
                IImageCodec codec = ImageCodec.GetRead(ImageCodec.FormatPng);

                // Try to use "The Secret Files" background
                string levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", "secretf", "01_easter1");
                if (!DirectoryOp.Exists(levelPath)) {
                    // Try to use "Base Game" background
                    levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", "prince", "03_carrot1");
                    if (!DirectoryOp.Exists(levelPath)) {
                        // Try to use "Holiday Hare '98" background
                        levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", "xmas98", "03_xmas3");
                        if (!DirectoryOp.Exists(levelPath)) {
                            // Try to use "Christmas Chronicles" background
                            levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", "xmas99", "03_xmas3");
                            if (!DirectoryOp.Exists(levelPath)) {
                                // Try to use "Shareware Demo" background;
                                levelPath = PathOp.Combine(DualityApp.DataDirectory, "Episodes", "share", "03_share3");
                                if (!DirectoryOp.Exists(levelPath)) {
                                    // No usable background found
                                    throw new FileNotFoundException();
                                }
                            }
                        }
                    }
                }

                // Load metadata
                JsonParser json = new JsonParser();
                LevelHandler.LevelConfigJson config;
                using (Stream s = FileOp.Open(PathOp.Combine(levelPath, ".res"), FileAccessMode.Read)) {
                    config = json.Parse<LevelHandler.LevelConfigJson>(s);
                }

                LevelHandler.LevelConfigJson.LayerSection layer;
                if (config.Layers.TryGetValue("Sky", out layer)) {
                    if (layer.BackgroundColor != null && layer.BackgroundColor.Count >= 3) {
                        horizonColor = new Vector4(layer.BackgroundColor[0] / 255f, layer.BackgroundColor[1] / 255f, layer.BackgroundColor[2] / 255f, 1f);
                    }
                }

                // Render background layer to texture
                string tilesetPath = PathOp.Combine(DualityApp.DataDirectory, "Tilesets", config.Description.DefaultTileset);

                ColorRgba[] tileMapPalette = TileSet.LoadPalette(PathOp.Combine(tilesetPath, ".palette"));
                ContentResolver.Current.ApplyBasePalette(tileMapPalette);

                TileSet levelTileset = new TileSet(config.Description.DefaultTileset);
                if (!levelTileset.IsValid) {
                    throw new InvalidDataException();
                }

                using (Stream s = FileOp.Open(PathOp.Combine(levelPath, "Sky.layer"), FileAccessMode.Read)) {
                    using (DeflateStream deflate = new DeflateStream(s, CompressionMode.Decompress))
                    using (BinaryReader r = new BinaryReader(deflate)) {

                        int width = r.ReadInt32();
                        int height = r.ReadInt32();

                        TileMapLayer newLayer = new TileMapLayer();
                        newLayer.Layout = new LayerTile[width * height];

                        for (int i = 0; i < newLayer.Layout.Length; i++) {
                            ushort tileType = r.ReadUInt16();

                            byte flags = r.ReadByte();
                            if (flags == 0) {
                                newLayer.Layout[i] = levelTileset.GetDefaultTile(tileType);
                                continue;
                            }

                            bool isFlippedX = (flags & 0x01) > 0;
                            bool isFlippedY = (flags & 0x02) > 0;
                            bool isAnimated = (flags & 0x04) > 0;
                            bool legacyTranslucent = (flags & 0x80) > 0;

                            // Invalid tile numbers (higher than tileset tile amount) are silently changed to empty tiles
                            if (tileType >= levelTileset.TileCount && !isAnimated) {
                                tileType = 0;
                            }

                            LayerTile tile;

                            // Copy the default tile and do stuff with it
                            tile = levelTileset.GetDefaultTile(tileType);
                            tile.IsFlippedX = isFlippedX;
                            tile.IsFlippedY = isFlippedY;
                            tile.IsAnimated = isAnimated;

                            if (legacyTranslucent) {
                                tile.MaterialAlpha = /*127*/140;
                            }

                            newLayer.Layout[i] = tile;
                        }

                        newLayer.LayoutWidth = width;

                        RecreateTexturedBackground(levelTileset, ref newLayer);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Cannot prerender textured background: " + ex);

                cachedTexturedBackground = new Texture(new Pixmap(new PixelData(2, 2, ColorRgba.Black)));
            }
        }

        private void RecreateTexturedBackground(TileSet levelTileset, ref TileMapLayer layer)
        {
            int w = layer.LayoutWidth;
            int h = layer.Layout.Length / w;

            Texture renderTarget;
            if (cachedTexturedBackground != null) {
                renderTarget = cachedTexturedBackground.Res;
            } else {
                renderTarget = new Texture(w * 32, h * 32, TextureSizeMode.NonPowerOfTwo, TextureMagFilter.Linear, TextureMinFilter.Linear, TextureWrapMode.Repeat, TextureWrapMode.Repeat);
                texturedBackgroundShader = ContentResolver.Current.RequestShader("TexturedBackground");
            }

            using (DrawDevice device = new DrawDevice()) {
                device.VisibilityMask = VisibilityFlag.AllFlags;
                device.RenderMode = RenderMatrix.ScreenSpace;

                device.Target = new RenderTarget(AAQuality.Off, false, renderTarget);
                device.TargetSize = new Vector2(w * 32, h * 32);
                device.ViewportRect = new Rect(device.TargetSize);

                device.PrepareForDrawcalls();

                // ToDo
                Material material = levelTileset.GetDefaultTile(0).Material.Res;
                Texture texture = material.MainTexture.Res;

                // Reserve the required space for vertex data in our locally cached buffer
                int neededVertices = 4 * w * h;
                VertexC1P3T2[] vertexData = new VertexC1P3T2[neededVertices];

                int vertexBaseIndex = 0;

                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        LayerTile tile = layer.Layout[x + y * layer.LayoutWidth];
                        if (tile.IsAnimated) {
                            continue;
                        }

                        Point2 offset = tile.MaterialOffset;
                        bool isFlippedX = tile.IsFlippedX;
                        bool isFlippedY = tile.IsFlippedY;

                        Rect uvRect = new Rect(
                            offset.X * texture.UVRatio.X / texture.ContentWidth,
                            offset.Y * texture.UVRatio.Y / texture.ContentHeight,
                            levelTileset.TileSize * texture.UVRatio.X / texture.ContentWidth,
                            levelTileset.TileSize * texture.UVRatio.Y / texture.ContentHeight
                        );

                        if (isFlippedX) {
                            uvRect.X += uvRect.W;
                            uvRect.W *= -1;
                        }
                        if (isFlippedY) {
                            uvRect.Y += uvRect.H;
                            uvRect.H *= -1;
                        }

                        Vector3 renderPos = new Vector3(x * 32, y * 32, 0);
                        float scale = 1.0f;
                        device.PreprocessCoords(ref renderPos, ref scale);

                        renderPos.X = MathF.Round(renderPos.X);
                        renderPos.Y = MathF.Round(renderPos.Y);
                        if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                            renderPos.X += 0.5f;
                        }
                        if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                            renderPos.Y += 0.5f;
                        }

                        Vector2 tileXStep = new Vector2(32, 0);
                        Vector2 tileYStep = new Vector2(0, 32);

                        vertexData[vertexBaseIndex + 0].Pos.X = renderPos.X;
                        vertexData[vertexBaseIndex + 0].Pos.Y = renderPos.Y;
                        vertexData[vertexBaseIndex + 0].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 0].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 0].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 0].Color = ColorRgba.White;

                        vertexData[vertexBaseIndex + 1].Pos.X = renderPos.X + tileYStep.X;
                        vertexData[vertexBaseIndex + 1].Pos.Y = renderPos.Y + tileYStep.Y;
                        vertexData[vertexBaseIndex + 1].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 1].TexCoord.X = uvRect.X;
                        vertexData[vertexBaseIndex + 1].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 1].Color = ColorRgba.White;

                        vertexData[vertexBaseIndex + 2].Pos.X = renderPos.X + tileXStep.X + tileYStep.X;
                        vertexData[vertexBaseIndex + 2].Pos.Y = renderPos.Y + tileXStep.Y + tileYStep.Y;
                        vertexData[vertexBaseIndex + 2].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 2].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 2].TexCoord.Y = uvRect.Y + uvRect.H;
                        vertexData[vertexBaseIndex + 2].Color = ColorRgba.White;

                        vertexData[vertexBaseIndex + 3].Pos.X = renderPos.X + tileXStep.X;
                        vertexData[vertexBaseIndex + 3].Pos.Y = renderPos.Y + tileXStep.Y;
                        vertexData[vertexBaseIndex + 3].Pos.Z = renderPos.Z;
                        vertexData[vertexBaseIndex + 3].TexCoord.X = uvRect.X + uvRect.W;
                        vertexData[vertexBaseIndex + 3].TexCoord.Y = uvRect.Y;
                        vertexData[vertexBaseIndex + 3].Color = ColorRgba.White;

                        vertexBaseIndex += 4;
                    }
                }

                device.AddVertices(material, VertexMode.Quads, vertexData, 0, vertexBaseIndex);

                device.Render();
            }

            cachedTexturedBackground = renderTarget;
        }

        private void RenderTexturedBackground(IDrawDevice device)
        {
            if (cachedTexturedBackground == null) {
                return;
            }

            float timeMult = Time.TimeMult;
            backgroundX += timeMult * 1.2f;
            backgroundY += timeMult * -0.2f + timeMult * MathF.Sin(backgroundPhase) * 0.6f;
            backgroundPhase += timeMult * 0.001f;

            Vector3 renderPos = new Vector3(0, 0, (device.NearZ + device.FarZ) * 0.5f);

            // Fit the target rect to actual pixel coordinates to avoid unnecessary filtering offsets
            renderPos.X = MathF.Round(renderPos.X);
            renderPos.Y = MathF.Round(renderPos.Y);
            if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2) {
                renderPos.X += 0.5f;
            }
            if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2) {
                //renderPos.Y += 0.5f;
                // AMD Bugfix?
                renderPos.Y -= 0.001f;
            }

            // Reserve the required space for vertex data in our locally cached buffer
            VertexC1P3T2[] vertexData;

            int neededVertices = 4;
            if (cachedVertices == null || cachedVertices.Length < neededVertices) {
                cachedVertices = vertexData = new VertexC1P3T2[neededVertices];
            } else {
                vertexData = cachedVertices;
            }

            // Render it as world-space fullscreen quad
            vertexData[0].Pos = new Vector3(renderPos.X, renderPos.Y, renderPos.Z);
            vertexData[1].Pos = new Vector3(renderPos.X + device.TargetSize.X, renderPos.Y, renderPos.Z);
            vertexData[2].Pos = new Vector3(renderPos.X + device.TargetSize.X, renderPos.Y + device.TargetSize.Y, renderPos.Z);
            vertexData[3].Pos = new Vector3(renderPos.X, renderPos.Y + device.TargetSize.Y, renderPos.Z);

            vertexData[0].TexCoord = new Vector2(0.0f, 0.0f);
            vertexData[1].TexCoord = new Vector2(1f, 0.0f);
            vertexData[2].TexCoord = new Vector2(1f, 1f);
            vertexData[3].TexCoord = new Vector2(0.0f, 1f);

            vertexData[0].Color = vertexData[1].Color = vertexData[2].Color = vertexData[3].Color = ColorRgba.White;

            // Setup custom pixel shader
            BatchInfo material = new BatchInfo(texturedBackgroundShader, cachedTexturedBackground);
            material.SetValue("horizonColor", horizonColor);
            material.SetValue("shift", new Vector2(backgroundX, backgroundY));

            device.AddVertices(material, VertexMode.Quads, vertexData);
        }
    }
}