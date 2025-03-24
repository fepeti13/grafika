﻿using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static uint program;

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
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing window: " + ex.Message);
            }
        }

        private static void GraphicWindow_Load()
        {
            try
            {
                Gl = graphicWindow.CreateOpenGL();
                Gl.ClearColor(System.Drawing.Color.White);

                uint vshader = Gl.CreateShader(ShaderType.VertexShader);
                uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

                Gl.ShaderSource(vshader, VertexShaderSource);
                Gl.CompileShader(vshader);
                Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
                if (vStatus != (int)GLEnum.True)
                    throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

                //Gl.ShaderSource(fshader, FragmentShaderSource);
                //Gl.CompileShader(fshader);
                Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
                if (fStatus != (int)GLEnum.True)
                    throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

                program = Gl.CreateProgram();
                Gl.AttachShader(program, vshader);
                Gl.AttachShader(program, fshader);
                Gl.LinkProgram(program);

                Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
                if (status == 0)
                    throw new Exception("Shader linking failed: " + Gl.GetProgramInfoLog(program));

                Gl.DetachShader(program, vshader);
                Gl.DetachShader(program, fshader);
                Gl.DeleteShader(vshader);
                Gl.DeleteShader(fshader);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during OpenGL initialization: " + ex.Message);
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            try
            {
                // No OpenGL operations here
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in update loop: " + ex.Message);
            }
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            try
            {
                Gl.Clear(ClearBufferMask.ColorBufferBit);

                uint vao = Gl.GenVertexArray();
                Gl.BindVertexArray(vao);

                float[] vertexArray = new float[]
                {
                    -0.5f, -0.5f, 0.0f,
                    +0.5f, -0.5f, 0.0f,
                     0.0f, +0.5f, 0.0f,
                     1f, 1f, 0f
                };

                float[] colorArray = new float[]
                {
                    1.0f, 0.0f, 0.0f, 1.0f,
                    0.0f, 1.0f, 0.0f, 1.0f,
                    0.0f, 0.0f, 1.0f, 1.0f,
                    1.0f, 0.0f, 0.0f, 1.0f,
                };

                uint[] indexArray = new uint[] {
                    0, 1, 2,
                    2, 1 ,3 
                };

                uint vertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);  
                Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(0);

                uint colors = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(1);

                uint indices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

                Gl.UseProgram(program);
                Gl.DrawElements(PrimitiveType.Triangles, (uint)indexArray.Length, DrawElementsType.UnsignedInt, null);

                Gl.BindVertexArray(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during rendering: " + ex.Message);
            }
        }
    }
}
