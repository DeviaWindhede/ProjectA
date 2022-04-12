using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class PNGGenerator : MonoBehaviour
{
    [SerializeField]
    private List<Sprite> sprites;

    private void Start()
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            Sprite s = sprites[i];
            // Read screen contents into the texture
            var r = s.textureRect;
            Color[] colors = s.texture.GetPixels(
                (int)r.xMin,
                (int)r.yMin,
                (int)r.xMax-(int)r.xMin,
                (int)r.yMax-(int)r.yMin
            );
            Texture2D tex = new Texture2D((int)r.xMax - (int)r.xMin, (int)r.yMax - (int)r.yMin);
            tex.SetPixels(0, 0, (int)r.xMax - (int)r.xMin, (int)r.yMax - (int)r.yMin, colors);
            tex.Apply();

            // Encode texture into PNG
            byte[] bytes = tex.EncodeToPNG();

            var dirPath = Application.dataPath + "/../SpritesToTexture2D/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            string path = dirPath + "Image" + i + ".png";
            File.WriteAllBytes(path, bytes);
            Debug.Log(path + " has been saved");
        }
    }
}
