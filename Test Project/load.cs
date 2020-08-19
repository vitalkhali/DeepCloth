using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class DataMats
{
    public float [] X_mu_vec {set;get;}
    public float [] Y_mu_vec {set;get;}
    public float [,] U {set;get;}
    public float [,] V {set;get;}
    public float [] alpha {set;get;}
    public float [] beta {set;get;}
    public float [,] Q {set;get;}
    public float [] norms_mu_vec {set;get;}
    public float [,] U_T {set;get;}
    public float [,] Q_T {set;get;}
}

public class ModelMats
{
    public float [,] fc1_weight {set;get;}
    public float [] fc1_bias {set;get;}
    public float [,] fc2_weight {set;get;}
    public float [] fc2_bias {set;get;}
    public float [,] fc3_weight {set;get;}
    public float [] fc3_bias {set;get;}
    public float [,] fc4_weight {set;get;}
    public float [] fc4_bias {set;get;}
    public float [,] fc5_weight {set;get;}
    public float [] fc5_bias {set;get;}
    public float [,] fc6_weight {set;get;}
    public float [] fc6_bias {set;get;}
    public float [,] fc7_weight {set;get;}
    public float [] fc7_bias {set;get;}
    public float [,] fc8_weight {set;get;}
    public float [] fc8_bias {set;get;}
    public float [,] fc9_weight {set;get;}
    public float [] fc9_bias {set;get;}
    public float [,] output_weight {set;get;}
    public float [] output_bias {set;get;}
}

public class TestLoad
{
    public DataMats dataMats;
    public ModelMats modelMats;
    public TestLoad()
    {
        TextAsset dataAsset = Resources.Load<TextAsset>("data_json");
        dataMats = JsonConvert.DeserializeObject<DataMats>(dataAsset.text);
        TextAsset modelAsset = Resources.Load<TextAsset>("model_json");
        modelMats = JsonConvert.DeserializeObject<ModelMats>(modelAsset.text);
        // Debug.Log(dataMats.alpha[127]);
        // Debug.Log(dataMats.U[127, 11]);
        // Debug.Log(modelMats.fc9_bias[3]);
    }
}
