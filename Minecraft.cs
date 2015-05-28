using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Exercise1
{
    public class Minecraft : GameWindow
    {
        #region Shader code
        string vertexShaderSource = @"
#version 330

precision highp float;

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform mat4 view_matrix;
uniform mat4 normal_matrix; //should be used is mat3(normal_matrix) later //is used for transforming normals(changes translation, but no rotation)

in vec3 in_position;
in vec2 in_tex;
in vec3 vertex_normal;

out vec2 tex2d; 
out vec3 camera_normal;
out vec3 camera_position;

void main(void)
{    
  tex2d = in_tex; 
  camera_normal = normalize(vec3(normal_matrix * vec4(vertex_normal,1)));
  camera_position = vec3(modelview_matrix * vec4(in_position,1));
  gl_Position = projection_matrix * modelview_matrix * vec4(in_position, 1);  
}";

        string fragmentShaderSource = @"
#version 330

precision highp float;

uniform sampler2D tex_image;
uniform mat4 modelview_matrix;
uniform mat4 view_matrix;

in vec2 tex2d; 
in vec3 normal;
in vec3 camera_normal;
in vec3 camera_position;

//Light position in world coordinate
vec3 light_pos_world = vec3(0, 1.5, 0);
  
//Light properties
vec3 light_spec = vec3(1, 1, 1);
vec3 light_diff = vec3(0.8, 0.8, 0.8);
vec3 light_amb = vec3(0.4, 0.4, 0.4);

//material properties
vec3 surface_spec = vec3(1, 1, 1);
vec3 surface_diff = vec3(1, 1, 1);  
vec3 surface_amb  = vec3(1, 1, 1);
float spec_exp = 400.0;

out vec4 frag_color;

void main(void)
{
    vec3 ambient_intens = light_amb * surface_amb;

    vec3 light_position_camera = vec3(view_matrix * vec4(-light_pos_world, 1)); // light in camera space
    vec3 camera_light_distance = light_position_camera - camera_position; //vector from fragment to light in camera space
    vec3 camera_light_direction = normalize(camera_light_distance);
    float cosine_normal_light = max(dot(camera_light_direction, camera_normal) ,0.0);     
    vec3 diff_intens = light_diff * surface_diff * cosine_normal_light;


    vec3 camera_reflected = reflect(-camera_light_direction, camera_normal);
    float spec_dot = max(dot(camera_reflected, normalize(-camera_position)), 0.0);
    float spec_factor = pow(spec_dot, spec_exp);
    vec3 spec_intens = light_spec * surface_spec * spec_factor;

    // retrieve texture color for fragment
    vec4 tex_color = texture(tex_image, tex2d.xy);
  
    frag_color = tex_color * vec4(ambient_intens + diff_intens + spec_intens ,1); 
}";
        #endregion

        #region Member variables

        int vertexShaderHandle,
            fragmentShaderHandle,
            shaderProgramHandle,
            modelviewMatrixLocation,
            projectionMatrixLocation,
            normalMatrixLocation,
            viewMatrixLocation;

        Stopwatch time = new Stopwatch();
        

        Matrix4 projectionMatrix, viewMatrix, normalMatrix; //normalMatrix is transposed inverse of mv-mat

        CubeFactory cubeFactory;
        Cube texturedCube;

        #endregion

        #region Constructor of window
        public Minecraft()
            : base(800, 600,
            new GraphicsMode(), "Tutorial: OpenGL 3.3", 0,
            DisplayDevice.Default, 3, 3,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        { }
        #endregion

        #region Initialization of Rendering 
        // Called after the GL context was created, but before entering main loop
        protected override void OnLoad(System.EventArgs e)
        {
            // Enable VSync to release CPU time during the render loop
            VSync = VSyncMode.On;

            // Set up general GL states
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(1.0f,1.0f,1.0f,1.0f);

            // Set up shaders
            CreateShaders();
            // Create objects to be rendered
            CreateObjects();

            Console.WriteLine("Minecraft " + GL.GetError().ToString());
        }


        void CreateShaders()
        {
            // Create shader
            vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(vertexShaderHandle, vertexShaderSource);
            GL.ShaderSource(fragmentShaderHandle, fragmentShaderSource);

            GL.CompileShader(vertexShaderHandle);
            GL.CompileShader(fragmentShaderHandle);

            Console.WriteLine(GL.GetShaderInfoLog(vertexShaderHandle));
            Console.WriteLine(GL.GetShaderInfoLog(fragmentShaderHandle));

            shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

            GL.BindAttribLocation(shaderProgramHandle, 0, "in_position");
            GL.BindAttribLocation(shaderProgramHandle, 1, "in_tex");
            GL.BindAttribLocation(shaderProgramHandle, 2, "vertex_normal");

            GL.LinkProgram(shaderProgramHandle);
            Console.WriteLine(GL.GetProgramInfoLog(shaderProgramHandle));

            GL.UseProgram(shaderProgramHandle);

            // Set uniforms
            projectionMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "projection_matrix");
            modelviewMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "modelview_matrix");
            normalMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "normal_matrix");
            viewMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "view_matrix");

            // Set projection matrix
            GL.Viewport(0, 0, Width, Height);
            float aspect = (float)Width / (float)Height;
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(0.7f, aspect, 0.1f, 100.0f);
            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);

            // For the start, set modelview matrix of shader to view matrix of opengl camera
            viewMatrix = Matrix4.LookAt(new Vector3(8.0f, 8.0f, 8.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f));
            GL.UniformMatrix4(viewMatrixLocation, false, ref viewMatrix);
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref viewMatrix);
            normalMatrix = Matrix4.Invert(viewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);


            // Set texture image location in shader to texture unit 0. We can do this, because
            // we will only use at most one texture per geometry. This texture is always bound 
            // to unit 0.
            int texImageLoc = GL.GetUniformLocation(shaderProgramHandle, "texImage");
            GL.Uniform1(texImageLoc, 0);
        }

        void CreateObjects()
        {
            // Initialize CubeFactory that can create cubes for us using the 
            // given texture.
            //cubeFactory = new CubeFactory("../../pattern.png");
            cubeFactory = new CubeFactory("../../texture_steve_new.png");
            // 
            texturedCube = cubeFactory.createTexturedCube();
            time.Start();
        }

        #endregion

        #region Release GL resources
        protected override void OnUnload(EventArgs e)
        {
            cubeFactory.ReleaseResources();
        }
        #endregion

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Handle input
            var keyboard = OpenTK.Input.Keyboard.GetState();
            if (keyboard[OpenTK.Input.Key.Escape])
                Exit();
        }
       
        Matrix4 rotMat = Matrix4.Identity;

        //LimitedRotation head = new LimitedRotation('y', false, 10);
        LimitedRotation rightArm = new LimitedRotation('x', true, 8);
        LimitedRotation leftArm = new LimitedRotation('x', false, 8);
        LimitedRotation rightLeg = new LimitedRotation('x', false, 8);
        LimitedRotation leftLeg = new LimitedRotation('x', true, 8);

        double count = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            /*
             * I think i get it now:
             * The only actual difference between fixed function and shader model is that we can reprogram the pipeline
             * When the drawing calls are made vertices are still just sent through the pipeline, which we provide the template for
            */

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.CullFace);
            // Render cube with modified scale 
            // Matrix4.CreateRotationY(10 * (float)Math.PI / 180) * Matrix4.CreateTranslation(DateTime.Now.Millisecond / 10 * (float)Math.PI / 180, 0f, 0f)

            rotMat = Matrix4.CreateRotationY((float)-Math.Sin(e.Time) / 1.3f) * rotMat;
            
            GL.BindVertexArray(texturedCube.VAOHandles[0]);
            //Head
            // Matrix4 modelviewMatrix = Matrix4.CreateScale(1f, 1f, 1f) * Matrix4.CreateTranslation(0f, 0f, 0f) * head.getRotMat(e.Time * 1.2f) * Matrix4.CreateTranslation(2f, 0f, 0f) * rotMat * viewMatrix; 
            Matrix4 modelviewMatrix = Matrix4.CreateScale(1f, 1f, 1f) * Matrix4.CreateTranslation(0f, -0.2f, 0f) * Matrix4.CreateRotationY((float)Math.Sin(count / 2) * 0.3f) * Matrix4.CreateTranslation(3f, 0f, 0f) * rotMat * viewMatrix; 
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();

            GL.BindVertexArray(texturedCube.VAOHandles[1]);
            //Body
            modelviewMatrix = Matrix4.CreateScale(1f, 1.5f, 0.8f) * Matrix4.CreateTranslation(3f, -1.5f, 0f)  * rotMat  * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();

            GL.BindVertexArray(texturedCube.VAOHandles[2]);
            //Right arm
            //Matrix4.CreateRotationX((float)Math.Sin(count)*0.4f)
            modelviewMatrix = Matrix4.CreateScale(0.5f, 1.5f, 0.5f) * Matrix4.CreateTranslation(0f, -0.75f, 0f) * Matrix4.CreateRotationX((float)-Math.Sin(count / 2) * 0.4f) * Matrix4.CreateTranslation(0f, 0.75f, 0f) * Matrix4.CreateTranslation(0.75f, -1.5f, 0f) * Matrix4.CreateTranslation(3f, 0f, 0f) * rotMat * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();


            GL.BindVertexArray(texturedCube.VAOHandles[2]);
            //Left arm
            modelviewMatrix = Matrix4.CreateScale(0.5f, 1.5f, 0.5f) * Matrix4.CreateTranslation(0f, -0.75f, 0f) * Matrix4.CreateRotationX((float)Math.Sin(count / 2) * 0.4f) * Matrix4.CreateTranslation(0f, 0.75f, 0f) * Matrix4.CreateTranslation(-0.75f, -1.5f, 0f) * Matrix4.CreateTranslation(3f, 0f, 0f) * rotMat * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();

            GL.BindVertexArray(texturedCube.VAOHandles[3]);
            //Right leg
            modelviewMatrix = Matrix4.CreateScale(0.5f, 1.5f, 0.5f) * Matrix4.CreateTranslation(0f, -1.3f, 0f) * Matrix4.CreateRotationX((float)Math.Sin(count / 2) * 0.4f) * Matrix4.CreateTranslation(0f, 1.3f, 0f) * Matrix4.CreateTranslation(0.25f, -3f, 0f) * Matrix4.CreateTranslation(3f, 0f, 0f) * rotMat * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();


            GL.BindVertexArray(texturedCube.VAOHandles[3]);
            //Left Leg
            modelviewMatrix = Matrix4.CreateScale(0.5f, 1.5f, 0.5f) * Matrix4.CreateTranslation(0f, -1.3f, 0f) * Matrix4.CreateRotationX((float)-Math.Sin(count / 2) * 0.4f) * Matrix4.CreateTranslation(0f, 1.3f, 0f) * Matrix4.CreateTranslation(-0.25f, -3f, 0f) * Matrix4.CreateTranslation(3f, 0f, 0f) * rotMat * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();


            GL.BindVertexArray(texturedCube.VAOHandles[4]);
            //floor
            //Stupid me, dont make the y scale of the plane 0, or it will make the matrix singular -> not invertible (|mvm|=0)
            modelviewMatrix = Matrix4.Identity * Matrix4.CreateScale(18f, 0.1f, 18f) * Matrix4.CreateTranslation(-2f, -4f, -2f) * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();


            GL.BindVertexArray(texturedCube.VAOHandles[5]);
            modelviewMatrix = Matrix4.Identity * Matrix4.CreateScale(0.1f, 0.1f, 0.1f) * Matrix4.CreateTranslation(0f, 1.5f, 0f) * viewMatrix;
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
            normalMatrix = Matrix4.Invert(modelviewMatrix);
            normalMatrix.Transpose();
            GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
            texturedCube.draw();


            count += 0.2;
            if (count >= Double.MaxValue - 10)
                count = 0;
            SwapBuffers();
        }

        //[STAThread]
        public static void Main()
        {
            using (Minecraft example = new Minecraft())
            {
                example.Title = "FH Salzburg | OpenGL 3 Tutorial";
                //example.Icon = OpenTK.Examples.Properties.Resources.Game;
                example.Run(60, 60);
            }
        }
    }
}

class LimitedRotation {
    private int rotationCounter;
    private Matrix4 rotMatrix;
    private sbyte reverse;
    private int tickLimit;
    private char axis { get; set; }

    public LimitedRotation(char _axis, bool _reverse, int ticks) {
       // rotationCounter = rotLength;
        rotationCounter = 0;
        rotMatrix = Matrix4.Identity;
        if (_reverse)
            reverse = -1;
        else
            reverse = 1;
        axis = _axis;
        tickLimit = ticks;
    }

    public Matrix4 getRotMat(double time) {
        if (rotationCounter >= tickLimit)
        {
            rotationCounter = -(2 * tickLimit);
            reverse *= -1;
        }
        setRotationMatrix(time);
        rotationCounter++;
        return rotMatrix;
    }

    private void setRotationMatrix(double time)
    {        
        switch (axis)
        {
            case 'y':
                rotMatrix = Matrix4.CreateRotationY((float)Math.Sin(time * reverse)) * rotMatrix;
                break;
            case 'x':
                rotMatrix = Matrix4.CreateRotationX((float)Math.Sin(time * reverse)) * rotMatrix;
                break;
            case 'z':
                rotMatrix = Matrix4.CreateRotationZ((float)Math.Sin(time * reverse)) * rotMatrix;
                break;
        } 
    }
}