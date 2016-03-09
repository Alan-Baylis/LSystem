using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public class Primitive {
    public char name;
    public GameObject prefab;
    public bool scaled;
};

[System.Serializable]
public class ProductionRule {
    public char initial;
    public string replacement;
    public float probability;
}

public class LSystem : MonoBehaviour {
    public float x_axis_rot_degree;
    public float y_axis_rot_degree;
    public uint iterations;
    public List<Primitive> primitives;
    public char initial;
    public List<ProductionRule> production_rules;

    //used for faster lookups
    private Dictionary<char, Primitive> primitive_map;
    private Dictionary<char, List<ProductionRule> > production_map;

    private const char branch_start = '[';
    private const char branch_end = ']';
    private const char rot_x_pos = '+';
    private const char rot_x_neg = '-';
    private const char rot_y_pos = '*';
    private const char rot_y_neg = '/';

    private System.Random rng;

    private string rewrite(string initial) {
        string output;
        StringBuilder builder = new StringBuilder();
        
        for(int i = 0; i < initial.Length; ++i) {
            if(production_map.ContainsKey(initial[i])) {
                List<ProductionRule> key_prod_rules = production_map[initial[i]];
                if(key_prod_rules.Count == 0) {
                    builder.Append(initial[i]);
                }
                else {
                    float total_prob = 0f;
                    float[] prob = new float[key_prod_rules.Count];

                    for(int j = 0; j < key_prod_rules.Count; ++j) {
                        total_prob += key_prod_rules[j].probability;
                        prob[j] = total_prob;
                    }

                    float rand_val = (float)rng.NextDouble();
                    rand_val *= total_prob;

                    int p = 0;
                    while(rand_val < prob[p] && p < prob.Length - 1) {
                        p++;
                    }

                    if(p > prob.Length - 1) {
                        p = prob.Length - 1;
                    }

                    string chosen_replacement = key_prod_rules[p].replacement;

                    builder.Append(chosen_replacement);
                }
            }
            else{
                builder.Append(initial[i]);
            }
        }

        output = builder.ToString();

        return output;
    }

    private float calculateXRot(string rot) {
        return 0;
    }

    private float calculateYRot(string rot) {
        return 0;
    }

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
