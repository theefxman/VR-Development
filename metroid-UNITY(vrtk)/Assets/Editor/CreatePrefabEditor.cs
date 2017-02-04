using UnityEngine;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

public class CreatePrefabEditor
{
    /* Example from Unity API Documentation for:
	 * PrefabUtility.CreateEmptyPrefab (Looks like a duplicate example) &
	 * PrefabUtiltiy.ReplacePrefab
	 * Converted from UnityScript to C#
	 * 
	 * However, if you make a prefab from the project folder, there are a few errors
	 * generated from the CreateNew() function.
	 * 
	 * Creates a prefab from the selected GameObjects.
	 * If the prefab already exists it asks if you want to replace it
	 * 
	 */


    public Transform player;
    public Transform floor_valid;
    public Transform floor_obstacle;
    public Transform floor_checkpoint;
    public GameObject childTileObject;

    public const string sfloor_valid = "0";
    public const string sfloor_obstacle = "1";
    public const string sfloor_checkpoint = "2";
    public const string sstart = "S";

    public static Transform lastChildPosition;
    public static float width;

    // Build a list to hold the multidimensional data mapped between NES tile byte assigments and Unity prefabs
    public static List<tileMapperList> tileMap = new List<tileMapperList>();



    //public const string br_m_platform_aqua = "";
    //public const string br_m_rock2_aqua = "";
    //public const string br_m_spiral_aqua = "";
    //public const string br_m_towers_aqua = "";

    public static string HexStr(byte[] p)
    {
        char[] c = new char[p.Length * 2 + 2];
        byte b;
        c[0] = '0'; c[1] = 'x';
        for (int y = 0, x = 2; y < p.Length; ++y, ++x)
        {
            b = ((byte)(p[y] >> 4));
            c[x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            b = ((byte)(p[y] & 0xF));
            c[++x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
        }
        return new string(c);
    }

    public static int nextOffset = 0;
    public static string structureName = "";
    public static string pAreaName = "";
    public static int structNum = 0;

    public static void structureBuilder(string fileName, int offset, string areaName)
    {
        // Set the public variable name for this function call... do this differently later :P
        pAreaName = areaName;

        // Setup the binary stream reader to take read in the hex values
        Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // Assign the virtual file to read the data stream into
        BinaryReader brFile = new BinaryReader(fileStream);

        // Set the initial position to read data from in the file using a hexadecimal offset (Where the data is in the file.
        // This is the first byte of the targetted structure)
        fileStream.Position = offset;

        // Read that first byte which says how many bytes are in that structures row (Likely values are 01-08)
        int numOfRowTiles = Convert.ToInt32(HexStr(brFile.ReadBytes(1)).Replace("0x", ""));

        // Set the offset to the macro data bytes of the first structure row to be read. This skips over the byte that contained
        // our numOfRowTiles value
        nextOffset = (Convert.ToInt32(offset.ToString("X"), 16) + 1);

        // Set a variable to monitor the datastream progression and stop if the data value returned is "FF". This value declares that
        // the structure is fully read into memory.                     
        string rowHeaderByte = "";

        // Create an array to store our structure row data in
        StringBuilder structure = new StringBuilder();

        // Look for more rows of the next structure until the byte F1 (brinstar, norfair) or A7 (tourian) or FF (ridley) is encountered
        while (rowHeaderByte != "F1" && rowHeaderByte != "A7" && rowHeaderByte != "FF")
        {
            // Look for more rows of this particular structure until the byte FF is encountered
            while (rowHeaderByte != "FF")
            {
                // Set the next offset position to read data from in the fileStream
                //fileStream.Position = nextOffset;


                //string lastOffset = nextOffset.ToString("X");
                string bytesRead = HexStr(brFile.ReadBytes(numOfRowTiles)).Replace("0x", "");

                // Append the current structure data row to the text file string builder
                if (numOfRowTiles > 0 && numOfRowTiles < 9)
                {
                    structure.AppendLine(Format(bytesRead, 2, " "));
                }
                else
                {
                    //  MessageBox.Show(numOfRowTiles + " <-- number to read, offset read --> " + lastOffset);
                    // break;
                }
                //MessageBox.Show("First Read Offset " + (Convert.ToInt32(fileStream.Position.ToString("X"), 16) - 1).ToString("X") + " , Bytes Read = " + numOfRowTiles+ " ,DATA = " + bytesRead);

                // Look at next immediate byte to see if it is FF or a value that indicates another row exists
                rowHeaderByte = HexStr(brFile.ReadBytes(1)).Replace("0x", "");


                if (rowHeaderByte != "FF")
                {
                    // Read that first byte of the next structure row which says how many bytes are in that structures row.
                    // (Likely values are 01-08)
                    if (Convert.ToInt32(rowHeaderByte) > 0 && Convert.ToInt32(rowHeaderByte) < 9)
                    {
                        numOfRowTiles = Convert.ToInt32(rowHeaderByte);
                    }
                    else
                    {
                        // MessageBox.Show("Inside FF check: " + rowHeaderByte + " <-- byte read, offset read --> " + fileStream.Position.ToString("X"));
                        //  break;
                    }
                }
            }

            //MessageBox.Show("OUTSIDE FF Offset " + (Convert.ToInt32(fileStream.Position.ToString("X"), 16) - 1).ToString("X") + " , Row Header Byte = " + rowHeaderByte);

            // Look for more rows of the next structure until the byte F1 (brinstar, norfair) or A7 (tourian) or FF (ridley) is encountered
            rowHeaderByte = HexStr(brFile.ReadBytes(1)).Replace("0x", "");
            if (rowHeaderByte != "F1" && rowHeaderByte != "A7" && rowHeaderByte != "FF")
            {
                numOfRowTiles = Convert.ToInt32(rowHeaderByte);
            }
            
            //MessageBox.Show("Outside FF next byte = " + numOfRowTiles.ToString() + " Offset = " + nextOffset.ToString("X"));

            // Write the structure data to a file on the system that reflects the actual structure byte value
            File.WriteAllText("Assets/Resources/struct/" + areaName + "/" + structNum.ToString("X2") + ".txt", structure.ToString());
            //MessageBox.Show("c:\\temp\\structure_" + structNum + "_" + areaName + ".txt <-- saving");

            // Increment the structure number count
            structNum++;

            // Reset the string builder object to clear out the previous structure
            structure.Length = 0;
        }
        Debug.Log("All structures read!");
    }


    public static void roomBuilder(string fileName, int offset, string areaName,int roomOffsetFactor)
    {
        // Setup the binary stream reader to take read in the hex values
        Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // Assign the virtual file to read the data stream into
        BinaryReader brFile = new BinaryReader(fileStream);
        fileStream.Position = offset;

        // Variable to store the nes XY coords. They store them Y then X for some reason..            
        string nesYXcoords = "";

        // Variable to store the structure byte read from the NES file/structure
        string structureRead = "";

        // Variable to store the palette/attribute byte
        string paletteRead = "";

        while (nesYXcoords != "FD" && nesYXcoords != "FF")
        {
            nesYXcoords = HexStr(brFile.ReadBytes(1)).Replace("0x", "");

            if (nesYXcoords != "FD" && nesYXcoords != "FF")
            {
                structureRead = HexStr(brFile.ReadBytes(1)).Replace("0x", "");

                paletteRead = HexStr(brFile.ReadBytes(1)).Replace("0x", "");

                Debug.Log("XY positionRead = " + nesYXcoords + " structure # = " + structureRead + " paletteRead =" + paletteRead + " offset was " + offset.ToString("X"));
                structurePreFabBuilder("Assets/Resources/struct/brinstar/" + structureRead + ".txt", int.Parse(nesYXcoords.Substring(1, 1), System.Globalization.NumberStyles.HexNumber) + roomOffsetFactor, int.Parse(nesYXcoords.Substring(0, 1), System.Globalization.NumberStyles.HexNumber));
            }
        }
        Debug.Log("All room data read!");
    }

    public static void structurePreFabBuilder(string fileName, int nesX, int nesY)
    {
        int scaleX = 2;
        int scaleY = 2;
        GameObject childTileObject = null;

        using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
        {

            // Declare a structure list to hold the bytes of our structure for indexing numerically later.
            List<structList> structure = new List<structList>();

            // Re-use the structure name from the file name. Later we may need other variables to say which 
            // CHR table data the structure is related to.
            string structName = Path.GetFileNameWithoutExtension(((FileStream)sr.BaseStream).Name);

            // Re-work the structname in case of duplicates
            structName = "Test_" + structName + "_" + (FindGameObjectsWithSameName(structName).Length + 1).ToString();

            // Make sure the plane created is set to 0,0,0 coords
            GameObject.Find("Plane").transform.position += new Vector3(0, 0, 0);
            

            // Create default parent
            var sceneParent = new GameObject(structName);
            

            // Generate our structure at specific X,Y,Z coords to match NES placement
            sceneParent.transform.position += new Vector3(nesX*2, nesY*2, 0);

            // Set the new parent object to the plane parent
            sceneParent.transform.SetParent(GameObject.Find("Plane").transform, true);

            string structureData = Regex.Replace(sr.ReadToEnd(), @"\t|\n|\r|", "");
            string[] structureBytes = structureData.Split(' ');
            foreach (string s in structureBytes)
            {
                if (s != "")
                {
                    structure.Add(new structList() { tileByte = s, structureName = structName });
                }
            }

            string text = System.IO.File.ReadAllText(fileName);
            string[] lines = Regex.Split(text, "\r\n");
            int rows = lines.Length;
            int byteCount = 0;

            string[][] jagged = new string[rows][];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] stringsOfLine = Regex.Split(lines[i], " ");
                jagged[i] = stringsOfLine;
            }

            //Debug.Log("Line1 = " + (jagged.Length-1).ToString() + "," + (jagged[0].Length-1).ToString());
            //Debug.Log("Line2 = " + (jagged.Length-1).ToString() + "," + (jagged[1].Length-1).ToString());
            //Debug.Log("Line3 = " + (jagged.Length-1).ToString() + "," + (jagged[2].Length-1).ToString());
            //Debug.Log("Line4 = " + (jagged.Length-1).ToString() + "," + (jagged[3].Length-1).ToString());

            // create planes based on matrix
            for (int y = 0; y < jagged.Length - 1; y++)
            {
                for (int x = 0; x < jagged[y].Length - 1; x++)
                {
                    var prefabName = "";
                    // Create the prefab in the UI editor/canvas
                    if (byteCount < structure.Count)
                    {
                        // Use this to see where the last structure failed to load
                        //Debug.Log("StructureName  " + structure[12].structureName);
                        Debug.Log("StructureTileByte  " + byteCount + " FileName > " + fileName);

                        prefabName = tileMap.Find(map => map.tileByte.Contains(structure[byteCount].tileByte)).preFabName;
                        byteCount++;

                        //Debug.Log(prefabName.Replace("_", "-") + "," + structure.Count + "," + byteCount);

                        switch (prefabName)
                        {


                            //case "br_m_tube_hori_aqua":
                            //    childTileObject = UnityEngine.Object.Instantiate(Resources.Load("br-m-tube-hori-aqua"), new Vector3(x * scaleX, (y * scaleY) - 1, 0), Quaternion.Euler(90, 270, 0)) as GameObject;
                            //    childTileObject.transform.SetParent(GameObject.Find(structName).transform, false);

                            //    break;
                            //case "br_m_rockR_aqua":
                            //    childTileObject = UnityEngine.Object.Instantiate(Resources.Load("br-m-rockR-aqua"), new Vector3((x * scaleX), ((y * scaleY)), 0), Quaternion.Euler(0, 180, 90)) as GameObject;
                            //    childTileObject.transform.SetParent(sceneParent.transform, false);
                            //    break;

                            //case "br_m_rockL_aqua":
                            //    childTileObject = UnityEngine.Object.Instantiate(Resources.Load("br-m-rockL-aqua"), new Vector3((x * scaleX), ((y * scaleY)), 0), Quaternion.Euler(0, 180, 90)) as GameObject;
                            //    childTileObject.transform.SetParent(sceneParent.transform, false);

                            //    //childTileObject.transform.SetParent(sceneParent.transform, false);

                            //    break;


                            default:

                                if (prefabName.Replace("_", "-") != "")
                                {
                                    //Debug.Log(structure.Count + "," + byteCount + "," + prefabName.Replace("_", "-") + "WTF");
                                    childTileObject = UnityEngine.Object.Instantiate(Resources.Load(prefabName.Replace("_", "-")), new Vector3(x * scaleX, y * scaleY, 0), Quaternion.Euler(0, 0, -180)) as GameObject;
                                    foreach (Transform child in childTileObject.transform)
                                    {
                                        child.transform.SetParent(childTileObject.transform, false);
                                        child.transform.position = new Vector3(childTileObject.transform.position.x, childTileObject.transform.position.y - 1, childTileObject.transform.position.z);
                                        //child.transform.localScale = childTileObject.transform.localScale;

                                    }

                                    //childTileObject.transform.Rotate(180, 0, 0);

                                    childTileObject.transform.SetParent(GameObject.Find(structName).transform, false);

                                }
                                break;
                        }

                    }
                }
            }
        }



    }

    public static string Format(string number, int batchSize, string separator)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i <= number.Length / batchSize; i++)
        {
            if (i > 0) sb.Append(separator);
            int currentIndex = i * batchSize;
            sb.Append(number.Substring(currentIndex,
                      Math.Min(batchSize, number.Length - currentIndex)));
        }
        return sb.ToString();
    }

    private static void loadRuntimeVariables()
    {

        // Add each related byte <-> prefab name mapping
        tileMap.Add(new tileMapperList() { tileByte = "00", preFabName = "br_c_lava_base_orange" });
        tileMap.Add(new tileMapperList() { tileByte = "01", preFabName = "br_c_lava_top_orange" });
        tileMap.Add(new tileMapperList() { tileByte = "02", preFabName = "br_c_hall_blue" });
        tileMap.Add(new tileMapperList() { tileByte = "03", preFabName = "br_m_tubeHori_hole_white" });
        tileMap.Add(new tileMapperList() { tileByte = "08", preFabName = "br_m_rock_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "0A", preFabName = "br_m_rockR_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "09", preFabName = "br_m_rockL_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "0C", preFabName = "br_m_statue_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "0D", preFabName = "br_c_balls_blue" });
        tileMap.Add(new tileMapperList() { tileByte = "17", preFabName = "br_m_pillar_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "1A", preFabName = "br_c_pillbrick_blue" });
        tileMap.Add(new tileMapperList() { tileByte = "1C", preFabName = "br-m-brush-aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "1D", preFabName = "br_m_bush_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "1F", preFabName = "br_m_pot_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "20", preFabName = "br_m_vent_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "22", preFabName = "br_c_ball" });
        tileMap.Add(new tileMapperList() { tileByte = "23", preFabName = "br_m_brick_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "28", preFabName = "br-m-balls-blue" });
        tileMap.Add(new tileMapperList() { tileByte = "30", preFabName = "br_c_bubble_lone_purple" });
        tileMap.Add(new tileMapperList() { tileByte = "33", preFabName = "br_c_foam_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "34", preFabName = "br_m_tube_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "35", preFabName = "br_m_tube_hori_aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "36", preFabName = "br_m_spiral_aqua" });

        tileMap.Add(new tileMapperList() { tileByte = "0B", preFabName = "br-m-rock2-aqua" });
        tileMap.Add(new tileMapperList() { tileByte = "06", preFabName = "br-m-seal-blue" });

        
    }

    public static GameObject[] FindGameObjectsWithSameName(string name)
    {
        GameObject[] allObjs = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
        List<GameObject> likeNames = new List<GameObject>();
        foreach (GameObject obj in allObjs)
        {
            if (obj.name.Contains(name))
            {
                likeNames.Add(obj);
            }
        }
        return likeNames.ToArray();
    }

    [MenuItem("MetroidVR/Create Scene")]
    private static void CreatePrefab()
    {
        
        
        
        // Load the static list of Prefab<->Structure mapping data
        loadRuntimeVariables();

        // Path to the nes rom for Metroid US
        //structureBuilder("c:\\temp\\test.data", 0x6C94, "Brinstar");
        //structureBuilder("c:\\temp\\test.data", 0xACC9, "Norfair");
        //structureBuilder("c:\\temp\\test.data", 0xEC26, "Tourian");
        //structureBuilder("c:\\temp\\test.data", 0x12A7B, "Kraid");
        //structureBuilder("c:\\temp\\test.data", 0x169CF, "Ridley");

        // Room 8 ( Farthest screen left in starting area )
        roomBuilder("Assets/Resources/test.data", 0x6598, "Brinstar", 0);
        //// Room 17 ( Screen with morph ball )
        roomBuilder("Assets/Resources/test.data", 0x6802, "Brinstar", 16);
        //// Room 9 (Starting Room)
        roomBuilder("Assets/Resources/test.data", 0x65CA, "Brinstar", 32);
        //// Room 14 ( 4th room from the right in starting area )
        roomBuilder("Assets/Resources/test.data", 0x679c, "Brinstar", 48);
        //// Room 13 ( Rightmost room with door in starting area )
        roomBuilder("Assets/Resources/test.data", 0x6779, "Brinstar", 64);

        //roomBuilder("Assets/Resources/test.data", 0x6464, "Brinstar", 64);
        GameObject.Find("Plane").transform.Rotate(180, 0, 0);

    }



    [MenuItem("MetroidVR/Create Scene", true)]
    bool ValidateCreatePrefab()
    {
        return Selection.activeGameObject != null;
    }

    // Create Empty Prefab and then Replace 
    static void CreateNew(GameObject obj, string localPath)
    {
        //UnityEngine.Object prefab = PrefabUtility.CreateEmptyPrefab(localPath);

        // Will ultimately create a game object mapping equal to the count of tiles in the structure
        GameObject childTileObject = UnityEngine.Object.Instantiate(Resources.Load("br-m-vent-aqua"), new Vector3(1000, 800, 1200), Quaternion.Euler(0, -180, 0)) as GameObject;

        //childTileObject.transform.SetParent(GameObject.Find("p-home").transform, false);

        //PrefabUtility.ReplacePrefab(obj, prefab, ReplacePrefabOptions.ConnectToPrefab);
    }

}