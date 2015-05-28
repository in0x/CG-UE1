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
     

    class Cube
    {
        #region Vertex data
        Vector3[] positionVboData = new Vector3[]{ 
            // bottom
            new Vector3(  0.5f, -0.5f, 0.5f),
            new Vector3( -0.5f, -0.5f, 0.5f),
            new Vector3(  0.5f, -0.5f, -0.5f),
                               
            new Vector3( -0.5f, -0.5f, -0.5f),
            new Vector3( -0.5f, -0.5f, 0.5f),
            new Vector3(  0.5f, -0.5f, -0.5f),

            // top 
            new Vector3(  0.5f, 0.5f, 0.5f),
            new Vector3(  0.5f, 0.5f, -0.5f),      
            new Vector3( -0.5f, 0.5f, 0.5f),  
      
            new Vector3( -0.5f, 0.5f, -0.5f),
            new Vector3( -0.5f, 0.5f, 0.5f),
            new Vector3(  0.5f, 0.5f, -0.5f),

            // front
            new Vector3( -0.5f, -0.5f, 0.5f),
            new Vector3(  0.5f, -0.5f, 0.5f),
            new Vector3(  0.5f, 0.5f, 0.5f),

            new Vector3( -0.5f, -0.5f, 0.5f),
            new Vector3(  0.5f, 0.5f, 0.5f),
            new Vector3( -0.5f, 0.5f, 0.5f),            

            // left
            new Vector3( -0.5f, -0.5f,  0.5f),
            new Vector3( -0.5f, 0.5f,  0.5f),
            new Vector3( -0.5f, -0.5f, -0.5f),

            new Vector3( -0.5f, -0.5f, -0.5f),
            new Vector3( -0.5f, 0.5f,  0.5f),
            new Vector3( -0.5f, 0.5f, -0.5f),

            // right
            new Vector3(  0.5f, -0.5f, 0.5f ),
            new Vector3(  0.5f, -0.5f, -0.5f),
            new Vector3(  0.5f, 0.5f, -0.5f),

            new Vector3(  0.5f, -0.5f,  0.5f),    
            new Vector3(  0.5f, 0.5f, -0.5f),
            new Vector3(  0.5f, 0.5f,  0.5f),
            // back
            new Vector3(  0.5f, -0.5f, -0.5f),
            new Vector3( -0.5f, -0.5f, -0.5f),
            new Vector3( -0.5f, 0.5f, -0.5f),

            new Vector3(  0.5f, -0.5f, -0.5f),
            new Vector3( -0.5f, 0.5f, -0.5f),
            new Vector3(  0.5f, 0.5f, -0.5f)
        };

        Vector3[] normalVboArr = new Vector3[36];
        Vector2[][] texVboArr = new Vector2[9][];
        Vector2[] texVboData = null;

        #endregion 

        #region Member variables

        int positionVboHandle,
            normalVboHandle,
            texture;

        int[] texVBOHandles = new int[6];
        public int[] VAOHandles = new int[6];

        #endregion

        #region Constructor
        public Cube(int textureId, Vector2[] texVboDataIn, Vector2[][] texVboArrIn)
        {
            texture = textureId;
            texVboData = texVboDataIn;
            texVboArr = texVboArrIn;
            for (int i = 0; i < positionVboData.Length; i+=6) {
                //Vector3 direction = Vector3.Cross(positionVboData[i + 1] - positionVboData[i], positionVboData[i + 2] - positionVboData[i]);
                Vector3 direction = Vector3.Cross(positionVboData[i + 1] - positionVboData[i], positionVboData[i + 2] - positionVboData[i]);
                for (int x = 0; x < 6; x++)
                    normalVboArr[i + x] = Vector3.Normalize(direction);
                //normalVboArr[i/6] = Vector3.Normalize(direction);

                //direction = Vector3.Cross(positionVboData[i] - positionVboData[i + 1], positionVboData[i+2] - positionVboData[i+1]);
                //normalVboArr[i + 1] = Vector3.Normalize(direction);

                //direction = Vector3.Cross(positionVboData[i + 1] - positionVboData[i + 2], positionVboData[i] - positionVboData[i + 2]);
                //normalVboArr[i + 2] = Vector3.Normalize(direction);
            }
            foreach (Vector3 vec in normalVboArr)
                Console.WriteLine(vec.ToString());
            CreateVBOs();
            CreateVAOs();
        }

        #endregion

        #region Initialization

        void CreateVBOs()
        {
            positionVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StaticDraw);

            for (int i = 0; i < texVBOHandles.Length; i++) {

                texVBOHandles[i] = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, texVBOHandles[i]);
                GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
                    new IntPtr(texVboArr[i+1].Length * Vector2.SizeInBytes),
                    texVboArr[i+1], BufferUsageHint.StaticDraw);
            }

            normalVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(normalVboArr.Length * Vector3.SizeInBytes),
                normalVboArr, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        void CreateVAOs()
        {
            for (int i = 0; i < VAOHandles.Length; i++) {
                VAOHandles[i] = GL.GenVertexArray();
                GL.BindVertexArray(VAOHandles[i]);


                GL.EnableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

                GL.EnableVertexAttribArray(1);
                GL.BindBuffer(BufferTarget.ArrayBuffer, texVBOHandles[i]);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);

                GL.EnableVertexAttribArray(2);
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
                GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

                GL.BindVertexArray(0);
            }
        }

        #endregion

        #region Rendering
        public void draw()
        {
            // Bind texture to unit 0 (as in shader)
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            // Bind vertex buffers
            //switch (toDraw.ToLower()) { 
            //    case "HEAD":
            //        GL.BindVertexArray(VAOHandles[1]);
            //}

            // Draw data as triangle
            GL.DrawArrays(PrimitiveType.Triangles, 0, positionVboData.Length);

            // Clean up
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindVertexArray(0);
        }
        #endregion
    }
}
