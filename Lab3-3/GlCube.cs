using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace GrafikaSzeminarium
{
    internal class GlCube
    {
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Normals { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }

        private GL Gl;

        private GlCube(uint vao, uint vertices, uint colors, uint normals, uint indices, uint indexArrayLength, GL gl)
        {
            this.Vao = vao;
            this.Vertices = vertices;
            this.Colors = colors;
            this.Normals = normals;
            this.Indices = indices;
            this.IndexArrayLength = indexArrayLength;
            this.Gl = gl;
        }

        public static unsafe GlCube CreateCubeWithFaceColors(GL Gl, float[] face1Color, float[] face2Color, float[] face3Color, float[] face4Color, float[] face5Color, float[] face6Color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // counter clockwise is front facing
            float[] vertexArray = new float[] {
                // Top face (Y+)
                -0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,

                // Front face (Z+)
                -0.5f, 0.5f, 0.5f,
                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,

                // Left face (X-)
                -0.5f, 0.5f, 0.5f,
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, 0.5f,

                // Bottom face (Y-)
                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,

                // Back face (Z-)
                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,

                // Right face (X+)
                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, 0.5f,
            };

            // Normal vectors for each face
            float[] normalArray = new float[] {
                // Top face normals (Y+)
                0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f,

                // Front face normals (Z+)
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f,

                // Left face normals (X-)
                -1.0f, 0.0f, 0.0f,
                -1.0f, 0.0f, 0.0f,
                -1.0f, 0.0f, 0.0f,
                -1.0f, 0.0f, 0.0f,

                // Bottom face normals (Y-)
                0.0f, -1.0f, 0.0f,
                0.0f, -1.0f, 0.0f,
                0.0f, -1.0f, 0.0f,
                0.0f, -1.0f, 0.0f,

                // Back face normals (Z-)
                0.0f, 0.0f, -1.0f,
                0.0f, 0.0f, -1.0f,
                0.0f, 0.0f, -1.0f,
                0.0f, 0.0f, -1.0f,

                // Right face normals (X+)
                1.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f,
            };

            List<float> colorsList = new List<float>();
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);

            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);

            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);

            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);

            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);

            float[] colorArray = colorsList.ToArray();

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            fixed (float* ptr = vertexArray)
            {
                Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertexArray.Length * sizeof(float)), ptr, GLEnum.StaticDraw);
            }
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            fixed (float* ptr = colorArray)
            {
                Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(colorArray.Length * sizeof(float)), ptr, GLEnum.StaticDraw);
            }
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint normals = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, normals);
            fixed (float* ptr = normalArray)
            {
                Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(normalArray.Length * sizeof(float)), ptr, GLEnum.StaticDraw);
            }
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, 0, null);
            Gl.EnableVertexAttribArray(2);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            fixed (uint* ptr = indexArray)
            {
                Gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(indexArray.Length * sizeof(uint)), ptr, GLEnum.StaticDraw);
            }

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)indexArray.Length;

            return new GlCube(vao, vertices, colors, normals, indices, indexArrayLength, Gl);
        }

        internal void ReleaseGlCube()
        {
            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Normals);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
        }
    }
}