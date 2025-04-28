using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using ImGuiNET;
using System.Numerics;

namespace Lab2
{
    public class ImGuiController : IDisposable
    {
        private readonly GL _gl;
        private readonly IWindow _window;
        private readonly IInputContext _input;
        private bool _frameBegun;
        private uint _vertexArray;
        private uint _vertexBuffer;
        private int _vertexBufferSize;
        private uint _indexBuffer;
        private int _indexBufferSize;
        private uint _fontTexture;
        private Shader _shader;
        private int _shaderFontTextureLocation;
        private int _shaderProjectionMatrixLocation;

        public ImGuiController(GL gl, IWindow window, IInputContext input)
        {
            _gl = gl;
            _window = window;
            _input = input;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            CreateDeviceObjects();
            SetKeyMappings();

            _window.Resize += WindowResized;
            _window.Closing += WindowClosing;
        }

        public void Dispose()
        {
            _window.Resize -= WindowResized;
            _window.Closing -= WindowClosing;
            ImGui.DestroyContext();
        }

        public void MakeCurrent() => ImGui.SetCurrentContext(ImGui.GetCurrentContext());

        public void Render()
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData());
            }
        }

        public void Update(double deltaSeconds)
        {
            if (_frameBegun)
                ImGui.Render();

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput();

            _frameBegun = true;
            ImGui.NewFrame();
        }

        private void WindowResized(Vector2D<int> size)
        {
            // Handle window resize
        }

        private void WindowClosing()
        {
            // Handle window closing
        }

        private void CreateDeviceObjects()
        {
            _vertexArray = _gl.GenVertexArray();
            _vertexBuffer = _gl.GenBuffer();
            _indexBuffer = _gl.GenBuffer();

            RecreateFontDeviceTexture();

            string VertexSource = @"#version 330 core
                layout (location = 0) in vec2 Position;
                layout (location = 1) in vec2 UV;
                layout (location = 2) in vec4 Color;
                uniform mat4 projection_matrix;
                out vec2 Frag_UV;
                out vec4 Frag_Color;
                void main()
                {
                    Frag_UV = UV;
                    Frag_Color = Color;
                    gl_Position = projection_matrix * vec4(Position.xy, 0, 1);
                }";

            string FragmentSource = @"#version 330 core
                in vec2 Frag_UV;
                in vec4 Frag_Color;
                uniform sampler2D Texture;
                layout (location = 0) out vec4 Out_Color;
                void main()
                {
                    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
                }";

            _shader = new Shader(_gl, VertexSource, FragmentSource);
            _shader.Use();

            _shaderProjectionMatrixLocation = _shader.GetUniformLocation("projection_matrix");
            _shaderFontTextureLocation = _shader.GetUniformLocation("Texture");

            _gl.BindVertexArray(_vertexArray);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);

            _gl.BufferData(BufferTargetARB.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 20, 0);

            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 20, 8);

            _gl.EnableVertexAttribArray(2);
            _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, 20, 16);

            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        private void SetKeyMappings()
        {
            var io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        }

        private void SetPerFrameImGuiData(double deltaSeconds)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(_window.Size.X, _window.Size.Y);
            io.DisplayFramebufferScale = Vector2.One;
            io.DeltaTime = (float)deltaSeconds;
        }

        private void UpdateImGuiInput()
        {
            var io = ImGui.GetIO();

            foreach (var mouse in _input.Mice)
            {
                io.MousePos = new Vector2(mouse.Position.X, mouse.Position.Y);
                io.MouseDown[0] = mouse.IsButtonPressed(MouseButton.Left);
                io.MouseDown[1] = mouse.IsButtonPressed(MouseButton.Right);
                io.MouseDown[2] = mouse.IsButtonPressed(MouseButton.Middle);
                io.MouseWheel = mouse.ScrollWheels[0].Y;
            }

            foreach (var keyboard in _input.Keyboards)
            {
                io.KeyCtrl = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
                io.KeyAlt = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight);
                io.KeyShift = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);
                io.KeySuper = keyboard.IsKeyPressed(Key.SuperLeft) || keyboard.IsKeyPressed(Key.SuperRight);
            }
        }

        private void RenderImDrawData(ImDrawDataPtr drawData)
        {
            if (drawData.CmdListsCount == 0)
            {
                return;
            }

            _gl.GetInteger(GetPName.Viewport, out int[] viewport);
            _gl.Viewport(0, 0, viewport[2], viewport[3]);

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = drawData.CmdListsRange[i];

                int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                if (vertexSize > _vertexBufferSize)
                {
                    int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                    _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                    _vertexBufferSize = newSize;
                }

                int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                    _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
                    _gl.BufferData(BufferTargetARB.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                    _indexBufferSize = newSize;
                }
            }

            _shader.Use();

            _gl.BindVertexArray(_vertexArray);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = drawData.CmdListsRange[n];

                _gl.BufferSubData(BufferTargetARB.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
                _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);

                int vtx_offset = 0;
                int idx_offset = 0;

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        _gl.ActiveTexture(TextureUnit.Texture0);
                        _gl.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                        _gl.Scissor((int)pcmd.ClipRect.X, viewport[3] - (int)pcmd.ClipRect.W, (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                        _gl.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(idx_offset * sizeof(ushort)), vtx_offset);
                    }

                    idx_offset += (int)pcmd.ElemCount;
                }
                vtx_offset += cmd_list.VtxBuffer.Size;
            }

            _gl.BindVertexArray(0);
            _gl.UseProgram(0);
        }

        private void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            _fontTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _fontTexture);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
            _gl.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            io.Fonts.SetTexID((IntPtr)_fontTexture);
            io.Fonts.ClearTexData();
        }
    }
} 