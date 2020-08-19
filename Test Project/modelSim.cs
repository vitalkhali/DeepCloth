using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Profiling;


public class modelSim : MonoBehaviour
{
    const int width = 43;
    const int height = 22;
    int vertexCnt;
    int triangleCnt;
    string path = "E:/TestProject/Test Project/Assets/model_info.txt";
    int parseCnt = 0;
    List<float> vertexData = new List<float>();
    List<int> triangleData = new List<int>();
    Mesh mesh;
    DataMats dataMats;
    ModelMats modelMats;
    float [] flattenVerts = new float[2838];
    float [] Zt_minus_1 = new float[128];
    float [] Zt_minus_2 = new float[128];
    float [] Zpred = new float[128]; 
    float [] Zcorrection = new float[128];
    
    [Range(-Mathf.PI / 2f, Mathf.PI / 2f)]
    public float windRot = 0.006f;
    [Range(1000f, 10000f)]
    public float windStrength = 6000f;
    
    float [] Yt = new float[2]{0.006f, 6000};
    float [] Wt = new float[2];
    float[] product = new float[2838];
    float[] tempArray = new float[2838];
    float [] mathTempArray = new float[2838];
    // Start is called before the first frame update
    void Start()
    {
        // Selection.activeGameObject = GameObject.Find("sim_model");
        // DestroyImmediate(Selection.activeGameObject);
        Material material = Resources.Load("Custom_NewSurfaceShader", typeof(Material)) as Material;
        mesh = gameObject.AddComponent<MeshFilter>().mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        StreamReader reader = new StreamReader(path);
        string[] data_lines = File.ReadAllLines(path);
        ParseData(data_lines);
        mesh.vertices = GenerateVertices();
        mesh.triangles = GenerateTriangles();  
        mesh.normals = GenerateNormals();
        Debug.Log("mesh created");  
        TestLoad testload = new TestLoad();
        // Debug.Log(testload.dataMats.U[127, 11]);
        // Debug.Log(testload.modelMats.fc9_bias[3]);
        dataMats = testload.dataMats;
        modelMats = testload.modelMats;
        // Forward();
        test();
    }

    void ParseData(string [] data_lines)
    {
        // int vertexCnt = 0; 
        int triangleCnt = 0;
        bool isTriangle = false;
        foreach(var line in data_lines)
        {
            if (line.Equals("separate")) isTriangle = true;
            else if(!isTriangle) vertexData.Add(float.Parse(line));
            
            else triangleData.Add(int.Parse(line));

        }
        vertexCnt = vertexData.Count / 3;
        triangleCnt = triangleData.Count / 3;
        // vertexCnt /= 3;
        // triangleCnt /= 3;
        // Debug.Log(vertexCnt);
        // Debug.Log(triangleCnt);
    }

    private int[] GenerateTriangles()
    {
        //generate two triangles per vertex except the last column and last row
        int[] triangles = triangleData.ToArray();
        return triangles;
    }
 
    private Vector3[] GenerateVertices()
    {
        Vector3[] vertices = new Vector3[vertexCnt];

        for (int outerCnt = 0; outerCnt < vertexCnt; outerCnt++)
        {
            vertices[outerCnt] = new Vector3(vertexData[3 * outerCnt], vertexData[3 * outerCnt + 1], vertexData[3 * outerCnt + 2]);
        }
        // for (int y = 0; y < height; y++ )
        // {
        //     for (int x = 0; x < width; x++ )
        //     {
        //         vertices[y * width + x] = new Vector3(x / (float)width, y / (float) height);
        //     }
        // }

        return vertices;
    }

    private Vector3[] GenerateNormals()
    {
        Vector3[] normals = new Vector3[vertexCnt];

        for (int outerCnt = 0; outerCnt < vertexCnt; outerCnt++)
        {
            normals[outerCnt] = new Vector3(1, 0, 0);
        }
        // for (int y = 0; y < height; y++ )
        // {
        //     for (int x = 0; x < width; x++ )
        //     {
        //         vertices[y * width + x] = new Vector3(x / (float)width, y / (float) height);
        //     }
        // }

        return normals;
    }

    void GetInputs(int cnt)
    {
        int vertCnt = 0;
        Profiler.BeginSample("get inputs");
        // mesh.vertices
        foreach(Vector3 vert in vertices)
        {
            flattenVerts[vertCnt] = vert.x - dataMats.X_mu_vec[vertCnt];
            flattenVerts[vertCnt + 1] = vert.y - dataMats.X_mu_vec[vertCnt + 1];
            flattenVerts[vertCnt + 2] = vert.z - dataMats.X_mu_vec[vertCnt + 2];
            vertCnt = vertCnt + 3;
        }
        Profiler.EndSample();
        MatrixMult(flattenVerts, 2838, dataMats.U);
        Array.Copy(product, 0, Zt_minus_1, 0, 128);

        // Debug.Log("first part: ");
        // Debug.Log(string.Join(",", Zt_minus_1));

        ComputeInitModel(cnt);
        Array.Copy(initResult, 0, Zpred, 0, 128);

        // Yt[1] = 6000 + UnityEngine.Random.Range(-1000, 1000);
        Yt[1] = windStrength + UnityEngine.Random.Range(-1000, 1000);
        Yt[1] = Mathf.Clamp(Yt[1], 1000, 10000);
        // Yt[0] = 0.006f + UnityEngine.Random.Range(-2, 2) * (Mathf.PI / 18f);
        Yt[0] = Mathf.Clamp(windRot, -Mathf.PI / 2f, Mathf.PI / 2f);




        int vecLen = VectorSub(Yt, dataMats.Y_mu_vec);
        MatrixMult(mathTempArray, 2, dataMats.V);
        Array.Copy(product, 0, Wt, 0, 2);
        //flatten input array
        Array.Copy(Zpred, 0, product, 0, 128);
        Array.Copy(Zt_minus_1, 0, product, 128, 128);
        Array.Copy(Wt, 0, product, 256, 2);

        //test
        float [] testInput = new float[258];
        Array.Copy(product, 0, testInput, 0, 258);
        // Debug.Log("input: ");
        // Debug.Log(string.Join(",", testInput));
    } 

    int cnt = 0;
    // Update is called once per frame
    void Update()
    {
        // Vector3[] vertices = mesh.vertices;
        // for (var i = 0; i < vertexCnt; i++)
        // {
        //     vertices[i] += Vector3.up * Time.deltaTime;
        // }

        // // assign the local vertices array into the vertices array of the Mesh.
        // mesh.vertices = vertices;
        // mesh.RecalculateBounds();   
        if (cnt >= 0)
        {
            ComputePipeline(cnt);
            // Debug.Log("output: ");
            // Debug.Log(string.Join(",", Zcorrection));
            // Debug.Log("cnt: " + cnt);
        }
        cnt ++;

    }



    void Forward()
    {
        // for (int i = 0; i < 258; i++)
        //     product[i] = i;
        
        MatrixMultWithBiasReLU(product, 258, modelMats.fc1_weight, modelMats.fc1_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc2_weight, modelMats.fc2_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc3_weight, modelMats.fc3_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc4_weight, modelMats.fc4_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc5_weight, modelMats.fc5_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc6_weight, modelMats.fc6_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc7_weight, modelMats.fc7_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc8_weight, modelMats.fc8_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.fc9_weight, modelMats.fc9_bias, true);
        MatrixMultWithBiasReLU(product, 192, modelMats.output_weight, modelMats.output_bias, false);
        // Debug.Log(product[5]);
    }

    void ComputePipeline(int cnt)
    {
        
        GetInputs(cnt);
        
        Forward();
        
        Array.Copy(product, 0, Zcorrection, 0, 128);
        
        int vecLen = VectorAdd(Zpred, Zcorrection);
        Array.Copy(mathTempArray, 0, Zpred, 0, vecLen);
        MatrixMult(Zpred, 128, dataMats.U_T);
        
        AssignVertices();
        
        //compute vertices' normals
        MatrixMult(Zpred, 128, dataMats.Q_T);
        
        AssignNormals();
        
        //update Zt_minus_1 and Zt_minus_2
        UpdateData();

    }

    int VectorElementWiseMult(float [] v1, float [] v2)
    {
        for (int i = 0; i < v1.GetLength(0); i++)
        {
            mathTempArray[i] = v1[i] * v2[i];
        }
        return v1.GetLength(0);        
    }

    int VectorAdd(float [] v1, float [] v2)
    {
        for (int i = 0; i < v1.GetLength(0); i++)
        {
            mathTempArray[i] = v1[i] + v2[i];
        }
        return v1.GetLength(0);
    }

    int VectorSub(float [] v1, float [] v2)
    {
        for (int i = 0; i < v1.GetLength(0); i++)
        {
            mathTempArray[i] = v1[i] - v2[i];
        }
        return v1.GetLength(0);
    }

    float [] initPart1 = new float[128];
    float [] initPart2 = new float[128];
    float [] initResult = new float[128];

    void ComputeInitModel(int cnt)
    {
        int vecLen = 0;
        if (cnt == 0)
            Array.Copy(Zt_minus_1, 0, Zt_minus_2, 0 ,128);

        vecLen = VectorSub(Zt_minus_1, Zt_minus_2);
        Array.Copy(mathTempArray, 0, initPart2, 0, vecLen);
        vecLen = VectorElementWiseMult(dataMats.beta, initPart2);
        Array.Copy(mathTempArray, 0, initPart2, 0, vecLen);

        vecLen = VectorElementWiseMult(dataMats.alpha, Zt_minus_1);
        Array.Copy(mathTempArray, 0, initPart1, 0, vecLen);

        vecLen = VectorAdd(initPart1, initPart2);
        Array.Copy(mathTempArray, 0, initResult, 0, vecLen);


        // Debug.Log("second part: ");
        // Debug.Log(string.Join(",", initPart2));
        
        // return VectorAdd(VectorElementWiseMult(dataMats.alpha, Zt_minus_1), VectorElementWiseMult(dataMats.beta, VectorSub(Zt_minus_1, Zt_minus_2)));
    }


    /// This method takes 3 matrices and one boolean variable as parameters
    /// (X is with dimension 1*m, weight matrix is with dimension m*n, bias is with dimension 1*n) 
    /// Compute ReLU(MatrixMult(X, weight) + bias) if bool ifActivate is set to true, 
    /// Otherwise, compute (MatrixMult(X, weight) + bias), 
    /// notice that for output layer, ifActivate should be set to false,
    /// the return is a matrix with dimension n*1,
    /// this computation is part of Neural Network's forward computation process
    void MatrixMultWithBiasReLU(float[] X, int XLen, float[,] weight, float[] bias, bool ifActivate)
    {
        if (XLen != weight.GetLength(0) || weight.GetLength(1) != bias.Length)
        {
            Debug.Log("XLen: " + XLen);
            Debug.Log("weight0: " + weight.GetLength(0));
            Debug.Log("weight1: " + weight.GetLength(1));
            Debug.Log("bias.Length: " + bias.Length);
            Debug.Log("Math error, please check parameters' dimensions");
        }
        else
        {

            /*
            During each loop, matrix multiplication is computed firstly,
            then bias is added to previous result,
            finally, ReLU activation function is applied.
            i in outer loop is the index of weight's column number,
            j in inner loop is the index of X's column number
            */
            Array.Copy(X, 0, tempArray, 0, XLen);
            for (int i = 0; i < weight.GetLength(1); i++)
            {
                float tempVal = 0;
                for (int j = 0; j < XLen; j++)
                {
                    tempVal += (tempArray[j] * weight[j, i]);
                }
                tempVal += bias[i];
                if (ifActivate)
                    product[i] = Mathf.Max(tempVal, 0);
                else
                    product[i] = tempVal;
            }
        }
    }
    void MatrixMult(float[] X, int XLen, float[,] weight)
    {
        if (XLen != weight.GetLength(0))
        {
            Debug.Log("XLen: " + XLen);
            Debug.Log("weight0: " + weight.GetLength(0));
            Debug.Log("weight1: " + weight.GetLength(1));
            Debug.Log("Math error, please check parameters' dimensions");
        }
        else
        {

            /*
            During each loop, matrix multiplication is computed firstly,

            i in outer loop is the index of weight's column number,
            j in inner loop is the index of X's column number
            */
            Array.Copy(X, 0, tempArray, 0, XLen);
            for (int i = 0; i < weight.GetLength(1); i++)
            {
                float tempVal = 0;
                for (int j = 0; j < XLen; j++)
                {
                    tempVal += (tempArray[j] * weight[j, i]);
                }

                product[i] = tempVal;
            }
        }
    }
    Vector3[] vertices = new Vector3[946];
    void AssignVertices()
    {
        // Profiler.BeginSample("assign vertex");
        // vertices = mesh.vertices;
        // Profiler.EndSample();
        // Debug.Log(vertexCnt);
        
        for (int i = 0; i < vertexCnt; i++)
        {
            // Debug.Log(i);
            // Debug.Log("debug: " + i * 3);
            vertices[i].x = product[i * 3] + dataMats.X_mu_vec[i * 3];
            vertices[i].y = product[i * 3 + 1] + dataMats.X_mu_vec[i * 3 + 1];
            vertices[i].z = product[i * 3 + 2] + dataMats.X_mu_vec[i * 3 + 2];
            // Debug.Log(vertices[i].x);
            // Debug.Log(vertices[i].y);
            // Debug.Log(vertices[i].z);
        }
        

        // assign the local vertices array into the vertices array of the Mesh.
        mesh.vertices = vertices;

        mesh.RecalculateBounds();
    }

    Vector3[] normals = new Vector3[946];
    void AssignNormals()
    {
        // normals = mesh.normals;
        // Profiler.BeginSample("assign normals");
        for (var i = 0; i < vertexCnt; i++)
        {
            normals[i].x = product[i * 3] + dataMats.norms_mu_vec[i * 3];
            normals[i].y = product[i * 3 + 1] + dataMats.norms_mu_vec[i * 3 + 1];
            normals[i].z = product[i * 3 + 2] + dataMats.norms_mu_vec[i * 3 + 2];
        }
        // Profiler.EndSample();

        // assign the local vertices array into the vertices array of the Mesh.
        mesh.normals = normals;
        mesh.RecalculateBounds();
    }

    void UpdateData()
    {
        // Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < 128; i++)
        {
            // Debug.Log(i);
            // Debug.Log("debug: " + i * 3);
            Zt_minus_2[i] = Zt_minus_1[i];

            Zt_minus_1[i] = Zpred[i];

        }

    }

    void test()
    {
        float [] test1 = new float[2]{1,5};
        float [] test2 = new float[2]{6,9};
        // Debug.Log("math test");
        // Debug.Log(string.Join(",", VectorSub(test1, test2)));
        // Debug.Log(string.Join(",", VectorAdd(test1, test2)));
        // Debug.Log(string.Join(",", VectorElementWiseMult(test1, test2)));
    }

}



// public class modelSim : MonoBehaviour
// {
//     public float width = 1;
//     public float height = 1;

//     public void Start()
//     {
//         MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
//         meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

//         MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

//         Mesh mesh = new Mesh();

//         Vector3[] vertices = new Vector3[4]
//         {
//             new Vector3(0, 0, 0),
//             new Vector3(width, 0, 0),
//             new Vector3(0, height, 0),
//             new Vector3(width, height, 0)
//         };
//         mesh.vertices = vertices;

//         int[] tris = new int[6]
//         {
//             // lower left triangle
//             0, 2, 1,
//             // upper right triangle
//             2, 3, 1
//         };
//         mesh.triangles = tris;

//         Vector3[] normals = new Vector3[4]
//         {
//             -Vector3.forward,
//             -Vector3.forward,
//             -Vector3.forward,
//             -Vector3.forward
//         };
//         mesh.normals = normals;

//         Vector2[] uv = new Vector2[4]
//         {
//             new Vector2(0, 0),
//             new Vector2(1, 0),
//             new Vector2(0, 1),
//             new Vector2(1, 1)
//         };
//         mesh.uv = uv;

//         meshFilter.mesh = mesh;
//     }
// }
