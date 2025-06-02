using Silk.NET.OpenGL;

namespace PirateShootingGame
{
    internal class GlObject
    {
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }
        public uint TextureId { get; private set; } = 0;
        public bool HasTexture => TextureId != 0;

        private GL Gl;

        public GlObject(uint vao, uint vertices, uint colors, uint indeces, uint indexArrayLength, GL gl, uint textureId = 0)
        {
            this.Vao = vao;
            this.Vertices = vertices;
            this.Colors = colors;
            this.Indices = indeces;
            this.IndexArrayLength = indexArrayLength;
            this.Gl = gl;
            this.TextureId = textureId;
        }

        public void BindTexture()
        {
            if (HasTexture)
            {
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, TextureId);
            }
        }

        public void SetTextureUniforms(uint program)
        {
            int hasTextureLoc = Gl.GetUniformLocation(program, "hasTexture");
            Gl.Uniform1(hasTextureLoc, HasTexture ? 1 : 0);

            if (HasTexture)
            {
                int textureLoc = Gl.GetUniformLocation(program, "diffuseTexture");
                Gl.Uniform1(textureLoc, 0);
            }
        }

        internal void ReleaseGlObject()
        {
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
            
            if (HasTexture)
            {
                Gl.DeleteTexture(TextureId);
            }
        }
    }
}