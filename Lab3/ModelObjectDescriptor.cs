using Silk.NET.OpenGL;
using System.Numerics;

namespace Szeminarium
{
    public class ModelObjectDescriptor : IDisposable
    {
        private bool disposedValue;

        public GL Gl { get; private set; }
        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public int IndexArrayLength { get; private set; }

        public static float width = 1.0f;
        public static float height = 2.0f;
        public static float depth = 0.1f;

        private ModelObjectDescriptor() { }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                 
                Gl.DeleteBuffer(Vertices);
                Gl.DeleteBuffer(Colors);
                Gl.DeleteBuffer(Indices);
                Gl.DeleteVertexArray(Vao);

                disposedValue = true;
            }
        }

        ~ModelObjectDescriptor()
        {
            Dispose(disposing: false);
        }

        public static ModelObjectDescriptor CreateCube(GL gl)
        {
             
             
            return CreateRectangleWithFlatNormals(gl, width, height, depth);
        }

         
        public static unsafe ModelObjectDescriptor CreateRectangleWithFlatNormals(GL gl, float width, float height, float depth)
        {
             
            uint vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

             
             
            float halfWidth = width / 2;
            float halfHeight = height / 2;
            float halfDepth = depth / 2;

             
             
            float[] vertexArray = {
                 
                -halfWidth, -halfHeight, halfDepth, 0, 0, 1,
                halfWidth, -halfHeight, halfDepth, 0, 0, 1,
                halfWidth, halfHeight, halfDepth, 0, 0, 1,
                -halfWidth, halfHeight, halfDepth, 0, 0, 1,

                 
                halfWidth, -halfHeight, -halfDepth, 0, 0, -1,
                -halfWidth, -halfHeight, -halfDepth, 0, 0, -1,
                -halfWidth, halfHeight, -halfDepth, 0, 0, -1,
                halfWidth, halfHeight, -halfDepth, 0, 0, -1,

                 
                -halfWidth, halfHeight, halfDepth, 0, 1, 0,
                halfWidth, halfHeight, halfDepth, 0, 1, 0,
                halfWidth, halfHeight, -halfDepth, 0, 1, 0,
                -halfWidth, halfHeight, -halfDepth, 0, 1, 0,

                 
                -halfWidth, -halfHeight, -halfDepth, 0, -1, 0,
                halfWidth, -halfHeight, -halfDepth, 0, -1, 0,
                halfWidth, -halfHeight, halfDepth, 0, -1, 0,
                -halfWidth, -halfHeight, halfDepth, 0, -1, 0,

                 
                halfWidth, -halfHeight, halfDepth, 1, 0, 0,
                halfWidth, -halfHeight, -halfDepth, 1, 0, 0,
                halfWidth, halfHeight, -halfDepth, 1, 0, 0,
                halfWidth, halfHeight, halfDepth, 1, 0, 0,

                 
                -halfWidth, -halfHeight, -halfDepth, -1, 0, 0,
                -halfWidth, -halfHeight, halfDepth, -1, 0, 0,
                -halfWidth, halfHeight, halfDepth, -1, 0, 0,
                -halfWidth, halfHeight, -halfDepth, -1, 0, 0
            };

             
            float[] colorArray = {
                 
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                 
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                 
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                 
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,

                 
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,

                 
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f
            };

             
            uint[] indexArray = {
                 
                0, 1, 2,
                0, 2, 3,

                 
                4, 5, 6,
                4, 6, 7,

                 
                8, 9, 10,
                8, 10, 11,

                 
                12, 13, 14,
                12, 14, 15,

                 
                16, 17, 18,
                16, 18, 19,

                 
                20, 21, 22,
                20, 22, 23
            };

             
            uint vertices = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            fixed (float* v = &vertexArray[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexArray.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }

             
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            gl.EnableVertexAttribArray(2);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

             
            uint colors = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
            fixed (float* c = &colorArray[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(colorArray.Length * sizeof(float)), c, BufferUsageARB.StaticDraw);
            }
            
             
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            gl.EnableVertexAttribArray(1);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

             
            uint indices = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            fixed (uint* i = &indexArray[0])
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexArray.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }

             
            gl.BindVertexArray(0);

            return new ModelObjectDescriptor 
            { 
                Gl = gl,
                Vao = vao, 
                Vertices = vertices, 
                Colors = colors, 
                Indices = indices, 
                IndexArrayLength = indexArray.Length 
            };
        }

         
        public static unsafe ModelObjectDescriptor CreateRectangleWithAngledNormals(GL gl, float width, float height, float depth)
        {
            uint vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            float halfWidth = width / 2;
            float halfHeight = height / 2;
            float halfDepth = depth / 2;

             
            float angleInRadians = 10.0f * MathF.PI / 180.0f;
            
             
             
            float zComponent = MathF.Cos(angleInRadians);
            float xComponent = MathF.Sin(angleInRadians);
            
             
             
            float[] vertexArray = {
                 
                -halfWidth, -halfHeight, halfDepth, -xComponent, 0, zComponent,
                halfWidth, -halfHeight, halfDepth, xComponent, 0, zComponent,
                halfWidth, halfHeight, halfDepth, xComponent, 0, zComponent,
                -halfWidth, halfHeight, halfDepth, -xComponent, 0, zComponent,

                 
                halfWidth, -halfHeight, -halfDepth, xComponent, 0, -zComponent,
                -halfWidth, -halfHeight, -halfDepth, -xComponent, 0, -zComponent,
                -halfWidth, halfHeight, -halfDepth, -xComponent, 0, -zComponent,
                halfWidth, halfHeight, -halfDepth, xComponent, 0, -zComponent,

                 
                -halfWidth, halfHeight, halfDepth, 0, 1, 0,
                halfWidth, halfHeight, halfDepth, 0, 1, 0,
                halfWidth, halfHeight, -halfDepth, 0, 1, 0,
                -halfWidth, halfHeight, -halfDepth, 0, 1, 0,

                 
                -halfWidth, -halfHeight, -halfDepth, 0, -1, 0,
                halfWidth, -halfHeight, -halfDepth, 0, -1, 0,
                halfWidth, -halfHeight, halfDepth, 0, -1, 0,
                -halfWidth, -halfHeight, halfDepth, 0, -1, 0,

                 
                halfWidth, -halfHeight, halfDepth, zComponent, 0, -xComponent,
                halfWidth, -halfHeight, -halfDepth, zComponent, 0, xComponent,
                halfWidth, halfHeight, -halfDepth, zComponent, 0, xComponent,
                halfWidth, halfHeight, halfDepth, zComponent, 0, -xComponent,

                 
                -halfWidth, -halfHeight, -halfDepth, -zComponent, 0, xComponent,
                -halfWidth, -halfHeight, halfDepth, -zComponent, 0, -xComponent,
                -halfWidth, halfHeight, halfDepth, -zComponent, 0, -xComponent,
                -halfWidth, halfHeight, -halfDepth, -zComponent, 0, xComponent
            };

             
            float[] colorArray = {
                 
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                 
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                 
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                 
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,

                 
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,

                 
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f
            };

             
            uint[] indexArray = {
                 
                0, 1, 2,
                0, 2, 3,
                
                 
                4, 5, 6,
                4, 6, 7,
                
                 
                8, 9, 10,
                8, 10, 11,
                
                 
                12, 13, 14,
                12, 14, 15,
                
                 
                16, 17, 18,
                16, 18, 19,
                
                 
                20, 21, 22,
                20, 22, 23
            };

             
            uint vertices = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            fixed (float* v = &vertexArray[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexArray.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }

             
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            gl.EnableVertexAttribArray(2);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

             
            uint colors = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
            fixed (float* c = &colorArray[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(colorArray.Length * sizeof(float)), c, BufferUsageARB.StaticDraw);
            }
            
             
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            gl.EnableVertexAttribArray(1);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

             
            uint indices = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            fixed (uint* i = &indexArray[0])
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexArray.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }

             
            gl.BindVertexArray(0);

            return new ModelObjectDescriptor 
            { 
                Gl = gl,
                Vao = vao, 
                Vertices = vertices, 
                Colors = colors, 
                Indices = indices, 
                IndexArrayLength = indexArray.Length 
            };
        }
    }
}