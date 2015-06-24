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

    // CubeFactory is a factory class that can be extended to create cubes that use
    // different texture coordinates, e.g. to index different parts of a texture
    class CubeFactory
    {
        #region Member variables

        int texture;

        #endregion

        #region Constructor/Release

        public CubeFactory(String textureName)
        {
            // Load texture only once and reuse it for all cubes
            CreateTexture(textureName);
        }

        public void ReleaseResources()
        {
            GL.DeleteTextures(1, ref texture);
        }

        #endregion

        #region Initialization
        void CreateTexture(String textureName)
        {
            // Load bitmap
            Bitmap bitmap = new Bitmap(textureName);
            // Flip image to match opengl tex coordinate system (0,0)->bottom left
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // Load texture
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);
        }
        #endregion

        #region Create simple textured cube
        //TODO: Top, Front, right done, -> texture rest
        public Cube createTexturedCube()
        {
            // Define texture coordinates of cube.
            // For Steve, this may correspond to the coordinates of the face texture.
            // Other cube instances can be created using other texture coordinates.

            //Here I pass texture coordinates for all parts as a jagged array into cube constructor
            //These could be calculated within the program, but i figured for so few of them I'll just read them out of the image

            Vector2[][] texVboArr = new Vector2[8][];

            //Reference Coordinates
            texVboArr[0] = new Vector2[]{ 
            // bottom
            new Vector2(1.0f, 0.0f),  new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f),        
            new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f),
            // top    
            new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f ),new Vector2(0.0f, 0.0f ), 
            new Vector2(0.0f,1.0f), new Vector2(0.0f,0.0f ), new Vector2(1.0f,1.0f ), 
            // front 
            new Vector2( 0.0f,0.0f), new Vector2( 1.0f,0.0f), new Vector2( 1.0f,1.0f),  
            new Vector2( 0.0f,0.0f), new Vector2( 1.0f,1.0f), new Vector2( 0.0f,1.0f),
            // left  
            new Vector2(1.0f, 0.0f ),new Vector2(1.0f, 1.0f ),new Vector2(0.0f, 0.0f),
            new Vector2( 0.0f, 0.0f),new Vector2( 1.0f, 1.0f), new Vector2( 0.0f, 1.0f),
            // right                  
            new Vector2( 0.0f, 0.0f),new Vector2( 1.0f, 0.0f),new Vector2( 1.0f, 1.0f),                      
            new Vector2( 0.0f, 0.0f),new Vector2( 1.0f, 1.0f),new Vector2( 0.0f, 1.0f),

            // back   
            new Vector2( 0.0f, 0.0f ), new Vector2( 1.0f, 0.0f ), new Vector2( 1.0f, 1.0f ),                   
            new Vector2(0.0f, 0.0f ),new Vector2(1.0f, 1.0f ),new Vector2(0.0f, 1.0f )
          };

        // Head Coordinates
        texVboArr[1] = new Vector2[]{
            // bottom
            new Vector2(0.375f, 0.0f),new Vector2(0.248f, 0.0f),new Vector2(0.375f, 1.0f),                  
            new Vector2(0.248f, 1.0f),new Vector2(0.248f, 0.0f),new Vector2(0.375f, 1.0f),

            // top    
            new Vector2( 0.245f,0.87f),new Vector2( 0.245f,1f),new Vector2( 0.125f,0.87f),                      
            new Vector2( 0.245f,1f),new Vector2( 0.125f,0.87f), new Vector2( 0.245f,1f),
                      
            //even newer front
            new Vector2( 0.108f,0.75f),new Vector2( 0.265f,0.75f),new Vector2( 0.265f,0.87f),                      
            new Vector2( 0.108f,0.75f),new Vector2( 0.265f,0.87f), new Vector2( 0.108f,0.87f),
                     
            // left  
            new Vector2(0.125f, 0.75f ),new Vector2(0.125f, 0.87f ),new Vector2(0.0f, 0.75f),                
            new Vector2( 0.0f, 0.75f),new Vector2( 0.125f, 0.87f), new Vector2( 0.0f, 0.87f),
                     
            // right                  
            new Vector2( 0.250f,0.75f),new Vector2( 0.375f,0.75f),new Vector2( 0.375f,0.87f),                    
            new Vector2( 0.250f,0.75f), new Vector2( 0.375f,0.87f), new Vector2( 0.250f,0.87f), 
                      
            // back   
            new Vector2( 0.375f, 0.75f ),new Vector2( 0.498f, 0.75f ),new Vector2( 0.498f,0.87f ),                    
            new Vector2(0.375f, 0.75f),new Vector2(0.498f, 0.87f ),new Vector2(0.375f, 0.87f )
        };

                //body coordinates
            texVboArr[2] = new Vector2[] { 
                // bottom
                new Vector2(1.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                    
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 1.0f),

                //// top 
                new Vector2( 0.44f,0.68f),
                new Vector2( 0.3125f,0.68f),
                new Vector2( 0.44f,0.75f),
                      
                new Vector2( 0.3125f,0.75f),
                new Vector2( 0.3125f,0.68f), 
                new Vector2( 0.44f, 0.75f),

                //// front 
                new Vector2( 0.3125f, 0.5f),
                new Vector2( 0.44f,0.5f),
                new Vector2( 0.44f,0.68f),
                      
                new Vector2( 0.3125f,0.5f),
                new Vector2( 0.44f,0.68f), 
                new Vector2( 0.3125f,0.68f),
         
                // left  
                new Vector2(0.3125f, 0.5f ),
                new Vector2(0.3125f, 0.65f ),
                new Vector2(0.25f, 0.5f),
                    
                new Vector2( 0.25f, 0.5f),
                new Vector2( 0.3125f, 0.68f), 
                new Vector2( 0.25f, 0.68f),
                 
                // right                  
                new Vector2( 0.2625f, 0.5f),
                new Vector2( 0.3125f, 0.5f),
                new Vector2( 0.3125f, 0.68f),
                      
                new Vector2( 0.2625f, 0.5f),
                new Vector2( 0.2625f, 0.68f), 
                new Vector2( 0.3125f, 0.68f),

                // back   
                new Vector2( 0.5f, 0.5f ),
                new Vector2( 0.625f, 0.5f ),
                new Vector2( 0.625f, 0.68f ),
                      
                new Vector2(0.5f, 0.5f ),
                new Vector2(0.625f, 0.68f ),
                new Vector2(0.5f, 0.68f )
             };

                //  arm coordinates
            texVboArr[3] = new Vector2[] { 
                // bottom
                
                new Vector2(0.8f, 0.0f),
                new Vector2(0.75f, 0.0f),
                new Vector2(0.8f, 1.0f),
                    
                new Vector2(0.75f, 1.0f),
                new Vector2(0.75f, 0.0f),
                new Vector2(0.8f, 1.0f),

                //// top 
                new Vector2( 0.69f,0.68f),
                new Vector2( 0.75f,0.68f),
                new Vector2( 0.69f,0.75f),
                      
                new Vector2( 0.75f,0.75f),
                new Vector2( 0.75f,0.68f), 
                new Vector2( 0.69f, 0.75f),

                ////// front 
                new Vector2( 0.69f, 0.5f),
                new Vector2( 0.75f,0.5f),
                new Vector2( 0.75f,0.68f),
                      
                new Vector2( 0.69f,0.5f),
                new Vector2( 0.75f,0.68f), 
                new Vector2( 0.69f,0.68f),

                // left  
                new Vector2(0.68f, 0.5f ),
                new Vector2(0.68f, 0.68f ),
                new Vector2(0.625f, 0.5f),
                    
                new Vector2( 0.625f, 0.5f),
                new Vector2( 0.68f, 0.68f), 
                new Vector2( 0.625f, 0.68f),

                // right                  
                new Vector2( 0.75f, 0.5f),
                new Vector2( 0.8f, 0.5f),
                new Vector2( 0.8f, 0.68f),
                      
                new Vector2( 0.75f, 0.5f),
                new Vector2( 0.8f, 0.68f), 
                new Vector2( 0.75f, 0.68f),

                // back   
                new Vector2( 0.82f, 0.5f ),
                new Vector2( 0.87f, 0.5f ),
                new Vector2( 0.87f, 0.68f ),
                      
                new Vector2(0.82f, 0.5f ),
                new Vector2(0.87f, 0.68f ),
                new Vector2(0.82f, 0.68f )
                };

                //legs
                texVboArr[4] = new Vector2[]{ 
                // bottom
                new Vector2(0.10f, 0.68f),
                new Vector2(0.05f, 0.68f),
                new Vector2(0.10f, 0.75f),
                    
                new Vector2(0.05f, 0.75f),
                new Vector2(0.05f, 0.68f),
                new Vector2(0.10f, 0.75f),

                // top    
                new Vector2(0.15f, 0.68f),
                new Vector2(0.10f, 0.68f),
                new Vector2(0.15f, 0.75f),
                    
                new Vector2(0.10f, 0.75f),
                new Vector2(0.10f, 0.68f),
                new Vector2(0.15f, 0.75f),
                    
                ////// front 
                new Vector2( 0.0625f, 0.5f),
                new Vector2( 0.125f,0.5f),
                new Vector2( 0.125f,0.68f),
                      
                new Vector2( 0.0625f,0.5f),
                new Vector2( 0.125f,0.68f), 
                new Vector2( 0.0625f,0.68f),
         
                // left  
                new Vector2(0.05f, 0.5f ),
                new Vector2(0.05f, 0.68f ),
                new Vector2(0.0f, 0.5f),
                    
                new Vector2( 0.0f, 0.5f),
                new Vector2( 0.05f, 0.68f), 
                new Vector2( 0.0f, 0.68f),
                     
                // right                  
                new Vector2( 0.12f, 0.5f),
                new Vector2( 0.18f, 0.5f),
                new Vector2( 0.18f, 0.68f),
                      
                new Vector2( 0.12f, 0.5f),                
                new Vector2( 0.18f, 0.68f),
                new Vector2( 0.12f, 0.68f),

                // back   
                new Vector2( 0.19f, 0.5f ),
                new Vector2( 0.25f, 0.5f ),
                new Vector2( 0.25f, 0.68f ),
                      
                new Vector2(0.19f, 0.5f ),
                new Vector2(0.25f, 0.68f ),
                new Vector2(0.19f, 0.68f )
             };

            //floor
           texVboArr[5] = new Vector2[]{ 
            
                // top    
                new Vector2(0.05f, 0.02f),   
                new Vector2(0.02f, 0.02f ),
                new Vector2(0.05f, 0.05f ),
                      
                new Vector2(0.02f,0.05f),
                new Vector2(0.02f,0.02f ),
                new Vector2(0.05f,0.05f ),
                //2
                new Vector2(0.05f, 0.02f),   
                new Vector2(0.02f, 0.02f ),
                new Vector2(0.05f, 0.05f ),
                      
                new Vector2(0.02f,0.05f),
                new Vector2(0.02f,0.02f ),
                new Vector2(0.05f,0.05f ),
                //3 front
                new Vector2(0.10f, 0.02f),   
                new Vector2(0.07f, 0.02f ),
                new Vector2(0.10f, 0.05f ),
                      
                new Vector2(0.07f,0.05f),
                new Vector2(0.07f,0.02f ),
                new Vector2(0.10f,0.05f ),
                //4
                new Vector2(0.10f, 0.02f),   
                new Vector2(0.07f, 0.02f ),
                new Vector2(0.10f, 0.05f ),
                      
                new Vector2(0.07f,0.05f),
                new Vector2(0.07f,0.02f ),
                new Vector2(0.10f,0.05f ),
                //5
                new Vector2(0.10f, 0.02f),   
                new Vector2(0.07f, 0.02f ),
                new Vector2(0.10f, 0.05f ),
                      
                new Vector2(0.07f,0.05f),
                new Vector2(0.07f,0.02f ),
                new Vector2(0.010f,0.05f ),
                //6
                new Vector2(0.05f, 0.02f),   
                new Vector2(0.02f, 0.02f ),
                new Vector2(0.05f, 0.05f ),
                      
                new Vector2(0.02f,0.05f),
                new Vector2(0.02f,0.02f ),
                new Vector2(0.05f,0.05f ),
            };

           texVboArr[6] = new Vector2[]{ 

                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),

                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),

                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),

                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),

                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),

                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f),
                new Vector2(0.08f, 0.05f)
            };

            Vector2[] texVboData = null;
            return new Cube(texture, texVboData, texVboArr);
        }
        #endregion


    }
}
