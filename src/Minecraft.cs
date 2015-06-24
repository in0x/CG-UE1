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
//A kingdom for syntax highlighting
precision highp float;

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform mat4 view_matrix; //needed for normal and vertex in camera space only
uniform mat4 normal_matrix; //should be used is mat3(normal_matrix) later //is used for transforming normals(changes translation, but no rotation)

in vec3 in_position;
in vec2 in_tex;
in vec3 vertex_normal;

//take in surface normal and vertex

out vec2 tex2d; 
out vec3 camera_normal;
out vec3 camera_position;

void main(void)
{    
    //Now also returning the normal(which is now transformed by the normal matrix) and the vertex position in camera space to fragment shader   
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
//since we are not using any complex, i.e. curved surfaces, it should fine to use the surface normal instead of interpolated vertex normals
in vec3 camera_normal;
in vec3 camera_position;

//Light position in world coordinate
vec3 light_pos_world = vec3(0, 1.5, 0);
  
//Light properties //Full bright specular light, softer diffuse and dimm ambient
vec3 light_spec = vec3(1, 1, 1);
vec3 light_diff = vec3(0.8, 0.8, 0.8);
vec3 light_amb = vec3(0.4, 0.4, 0.4);

//material properties //influence how strongly each light component is reflected
vec3 surface_spec = vec3(1, 1, 1);
vec3 surface_diff = vec3(1, 1, 1);  
vec3 surface_amb  = vec3(1, 1, 1);
float spec_exp = 400.0;

out vec4 frag_color;

void main(void)
{
    //Factor of reflected ambient
    vec3 ambient_intens = light_amb * surface_amb;

    
    vec3 light_position_camera = vec3(view_matrix * vec4(-light_pos_world, 1)); // light in camera space //- since we need to use a light vector pointing from vertex for angle 
    vec3 camera_light_distance = light_position_camera - camera_position; //vector from fragment to light in camera space
    vec3 camera_light_direction = normalize(camera_light_distance);

    //if light hits at a negative 
    //angle we need to use 0.0 instead so as to not produce a negative diffuse factor
    //both vectors are normalized already so no need to divide by product of lengths for cosine factor
    float cosine_normal_light = max(dot(camera_light_direction, camera_normal) ,0.0);     
    vec3 diff_intens = light_diff * surface_diff * cosine_normal_light;

    //returns us a vector which is the first argument reflected by the second argument
    vec3 camera_reflected = reflect(-camera_light_direction, camera_normal);
    
    //get the angle between the reflected light vector and the camera-vertex vector
    float spec_dot = max(dot(camera_reflected, normalize(-camera_position)), 0.0);

    //Calculate specular light factor with angle between the ray and the shininess factor
    float spec_factor = pow(spec_dot, spec_exp);
    vec3 spec_intens = light_spec * surface_spec * spec_factor;

    // retrieve texture color for fragment
    vec4 tex_color = texture(tex_image, tex2d.xy);
  
    //Now create a new fragment color which is influenced by light
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

        //normalMatrix is transposed inverse of mv-mat, used for recalculating normals after transformation
        Matrix4 projectionMatrix, viewMatrix, normalMatrix;  

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

            // For the start, set modelview matrix of shader to view matrix of opengl camera, normal_matrix only passed for testing here
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

            //Each of these calls boils down to the same pattern:
            /*
             *Bind the appropriate VAO for the different texture coordinates
             *Then adjust the scale of the cube
             *Bring it into the correct position for the body
             *Apply a Sine-based Rotation for movement back and forth (opposing parts have a reversed rotation to mimmick walking)
             *Move object out to change center of rotation
             *Apply rotation to create movement in circle around center
             *Apply viewMatrix
             *Create new normal matrix based on changed mv-matrix and pass it into the uniform
             *Issue draw call
             *Floor and light position do not share the constant transformations
             */

            rotMat = Matrix4.CreateRotationY((float)-Math.Sin(e.Time) / 1.3f) * rotMat;
            
            GL.BindVertexArray(texturedCube.VAOHandles[0]);
            //Head
            // Matrix4 modelviewMatrix = Matrix4.CreateScale(1f, 1f, 1f) * Matrix4.CreateTranslation(0f, 0f, 0f) * head.getRotMat(e.Time * 1.2f) * Matrix4.CreateTranslation(2f, 0f, 0f) * rotMat * viewMatrix; 
            Matrix4 modelviewMatrix = Matrix4.CreateScale(1f, 1f, 1f) * Matrix4.CreateTranslation(0f, -0.2f, 0f) * Matrix4.CreateRotationY((float)Math.Sin(count / 2) * 0.5f) * Matrix4.CreateTranslation(3f, 0f, 0f) * rotMat * viewMatrix; 
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
