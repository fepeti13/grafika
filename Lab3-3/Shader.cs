using Silk.NET.OpenGL;
using System.Numerics;

namespace Lab2
{
    public class Shader : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        public Shader(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;
            _handle = _gl.CreateProgram();

            uint vertex = LoadShader(ShaderType.VertexShader, vertexSource);
            uint fragment = LoadShader(ShaderType.FragmentShader, fragmentSource);

            _gl.AttachShader(_handle, vertex);
            _gl.AttachShader(_handle, fragment);
            _gl.LinkProgram(_handle);

            _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
            }

            _gl.DetachShader(_handle, vertex);
            _gl.DetachShader(_handle, fragment);
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
        }

        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        public void SetUniform(string name, int value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, float value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, Matrix4x4 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            unsafe
            {
                _gl.UniformMatrix4(location, 1, false, (float*)&value);
            }
        }

        public void SetUniform(string name, Vector3 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Vector4 value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }

        public int GetUniformLocation(string name)
        {
            return _gl.GetUniformLocation(_handle, name);
        }

        private uint LoadShader(ShaderType type, string source)
        {
            uint handle = _gl.CreateShader(type);
            _gl.ShaderSource(handle, source);
            _gl.CompileShader(handle);

            string infoLog = _gl.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
} 