using Silk.NET.OpenGL;
using System.Numerics;

namespace Lab2
{
    public class GlCube
    {
        private readonly GL _gl;
        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;
        private readonly int _indexArrayLength;

        public uint Vao => _vao;
        public int IndexArrayLength => _indexArrayLength;

        private GlCube(GL gl, uint vao, uint vbo, uint ebo, int indexArrayLength)
        {
            _gl = gl;
            _vao = vao;
            _vbo = vbo;
            _ebo = ebo;
            _indexArrayLength = indexArrayLength;
        }

        public static GlCube CreateCubeWithFaceColors(GL gl, float[] face1, float[] face2, float[] face3, float[] face4, float[] face5, float[] face6)
        {
            float[] vertices = new float[]
            {
                // Front face (red)
                -0.5f, -0.5f,  0.5f, face1[0], face1[1], face1[2], face1[3],  0.0f,  0.0f,  1.0f,
                 0.5f, -0.5f,  0.5f, face1[0], face1[1], face1[2], face1[3],  0.0f,  0.0f,  1.0f,
                 0.5f,  0.5f,  0.5f, face1[0], face1[1], face1[2], face1[3],  0.0f,  0.0f,  1.0f,
                -0.5f,  0.5f,  0.5f, face1[0], face1[1], face1[2], face1[3],  0.0f,  0.0f,  1.0f,

                // Back face (orange)
                -0.5f, -0.5f, -0.5f, face2[0], face2[1], face2[2], face2[3],  0.0f,  0.0f, -1.0f,
                 0.5f, -0.5f, -0.5f, face2[0], face2[1], face2[2], face2[3],  0.0f,  0.0f, -1.0f,
                 0.5f,  0.5f, -0.5f, face2[0], face2[1], face2[2], face2[3],  0.0f,  0.0f, -1.0f,
                -0.5f,  0.5f, -0.5f, face2[0], face2[1], face2[2], face2[3],  0.0f,  0.0f, -1.0f,

                // Left face (blue)
                -0.5f, -0.5f, -0.5f, face3[0], face3[1], face3[2], face3[3], -1.0f,  0.0f,  0.0f,
                -0.5f, -0.5f,  0.5f, face3[0], face3[1], face3[2], face3[3], -1.0f,  0.0f,  0.0f,
                -0.5f,  0.5f,  0.5f, face3[0], face3[1], face3[2], face3[3], -1.0f,  0.0f,  0.0f,
                -0.5f,  0.5f, -0.5f, face3[0], face3[1], face3[2], face3[3], -1.0f,  0.0f,  0.0f,

                // Right face (green)
                 0.5f, -0.5f, -0.5f, face4[0], face4[1], face4[2], face4[3],  1.0f,  0.0f,  0.0f,
                 0.5f, -0.5f,  0.5f, face4[0], face4[1], face4[2], face4[3],  1.0f,  0.0f,  0.0f,
                 0.5f,  0.5f,  0.5f, face4[0], face4[1], face4[2], face4[3],  1.0f,  0.0f,  0.0f,
                 0.5f,  0.5f, -0.5f, face4[0], face4[1], face4[2], face4[3],  1.0f,  0.0f,  0.0f,

                // Top face (white)
                -0.5f,  0.5f,  0.5f, face5[0], face5[1], face5[2], face5[3],  0.0f,  1.0f,  0.0f,
                 0.5f,  0.5f,  0.5f, face5[0], face5[1], face5[2], face5[3],  0.0f,  1.0f,  0.0f,
                 0.5f,  0.5f, -0.5f, face5[0], face5[1], face5[2], face5[3],  0.0f,  1.0f,  0.0f,
                -0.5f,  0.5f, -0.5f, face5[0], face5[1], face5[2], face5[3],  0.0f,  1.0f,  0.0f,

                // Bottom face (yellow)
                -0.5f, -0.5f,  0.5f, face6[0], face6[1], face6[2], face6[3],  0.0f, -1.0f,  0.0f,
                 0.5f, -0.5f,  0.5f, face6[0], face6[1], face6[2], face6[3],  0.0f, -1.0f,  0.0f,
                 0.5f, -0.5f, -0.5f, face6[0], face6[1], face6[2], face6[3],  0.0f, -1.0f,  0.0f,
                -0.5f, -0.5f, -0.5f, face6[0], face6[1], face6[2], face6[3],  0.0f, -1.0f,  0.0f,
            };

            uint[] indices = new uint[]
            {
                0, 1, 2, 2, 3, 0,    // Front
                4, 5, 6, 6, 7, 4,    // Back
                8, 9, 10, 10, 11, 8, // Left
                12, 13, 14, 14, 15, 12, // Right
                16, 17, 18, 18, 19, 16, // Top
                20, 21, 22, 22, 23, 20  // Bottom
            };

            uint vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            uint vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

            uint ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

            // Position attribute
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 10 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(0);

            // Color attribute
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 10 * sizeof(float), (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);

            // Normal attribute
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 10 * sizeof(float), (void*)(7 * sizeof(float)));
            gl.EnableVertexAttribArray(2);

            return new GlCube(gl, vao, vbo, ebo, indices.Length);
        }

        public void ReleaseGlCube()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
        }
    }
}