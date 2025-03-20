using System.Buffers;
using System.Drawing;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szem1{
    internal class Program{
        private static IWindow graphicWindow;
        
        private static GL Gl;

        private static uint program;


        //ha bejon egy pont, megmondja, hogy az hol kene legyen

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Load()
        {
            //inicializalo dolgok, egyszer hivodik meg
            
            Console.WriteLine("Loaded.");
            Gl = graphicWindow.CreateOpenGL();

            //beallitjuk a hatterszint
            //Gl.ClearColor(System.Drawing.Color.White);

             Gl.ClearColor(System.Drawing.Color.White);

            Gl.Enable(EnableCap.CullFace); //kikapcsoljuk, hogy a hatat ne renderelje ki a dolgoknak
            Gl.CullFace(TriangleFace.Back);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();           //megmondjuk, hogy hozzon letre egy programot, megmutatjuk, hogy melyik eleme mi legyen
            Gl.AttachShader(program, vshader);       
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.DetachShader(program, vshader);      //levalasztjuk, ha mar megvan
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            //NO OpenGL kod itt
            //szalbiztos kod, kell legyen
            //Console.WriteLine($"Update after {deltaTime} seconds");
        }

        private static unsafe void GraphicWindow_Render(double deltaTime) //kezzel be kell allitani, hogy ez unsafe kod
        {
            //ide jon az, hogy effektiv ki szeretnenk rajzolni valamit
            //Console.WriteLine($"Render after {deltaTime} seconds");

            //meg kell mondani, hogy renderelje ujra a kepernyot mert megvaltozott a keprnyo szine
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            //a vertex arrayek azok, ahova tulajdonsagokat lehet bepakolni tomszerint
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            //ki szeretnenk lerajzolni az alabbi negy pont kozotti reszt
            float[] vertexArray = new float[] { //itt vannak a csucsok
                -0.5f, -0.5f, 0.0f,   //x, y, z kordinatak
                +0.5f, -0.5f, 0.0f,
                 0.0f, +0.5f, 0.0f,
                 1f, 1f, 0f
            };

            float[] colorArray = new float[] {  //megadja a csucsok szinet
                1.0f, 0.0f, 0.0f, 1.0f,  //rgb + alpha - atlathatosag
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
            };
            
            uint[] indexArray = new uint[] {  //a csucsok sorrendjet adja meg
                0, 1, 2,        //haromszogeket fogunk kirajzolni
                2, 1, 3         //ilyen sorrendben
            };


            uint vertices = Gl.GenBuffer();    //hozz letre egy buffert
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);   //kosd be az arraybufferhez
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw); //az array bufferbe toltsd be ennek a buffernek az adatat
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);    //beallitunk egy vertexAttributum pointert, ez egy tomb, aminek a nulladik helyere fogunk betenni,
                                                                                            //ezt 3 Floatosavval kell ertelzeni, megkerdi, hgoy normalizaltak-e az ertekek
            Gl.EnableVertexAttribArray(0);      //bekapcsoljuk a nulladik tulajdonsagat ennek a vertex objectnek
                                                //nem kell mindenik bekapcsolva legyen
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);


            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null); //1-es poziciora tesszszuk, es nem kell normalizalni mert szinek
            Gl.EnableVertexAttribArray(1);     //ezt is bekapcsoljuk
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            //rajzolas resz
            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
        
            //draw
            //szukseg lesz egy kodra ami a grafikus kartyan fut
            //a grafikus kartyak is Neumann arhitekturat kovetnek
        
            Gl.UseProgram(program);         //hasznaljuk a fent definialt programot
            
            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vao);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);


        }

    }
}