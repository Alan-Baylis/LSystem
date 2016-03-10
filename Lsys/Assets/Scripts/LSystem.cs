using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

[System.Serializable]
public class Primitive {
    public char name;
    public GameObject prefab;
    public bool ignore_scale;
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
    public string initial;
    public List<ProductionRule> production_rules;

    //used for faster lookups
    private Dictionary<char, Primitive> primitive_map;
    private Dictionary<char, List<ProductionRule> > production_map;

    private const char BRANCH_START = '[';
    private const char BRANCH_END = ']';
    private const char ROT_X_POS = '+';
    private const char ROT_X_NEG = '-';
    private const char ROT_Y_POS = '*';
    private const char ROT_Y_NEG = '/';

    private System.Random rng;

    private string tree_string;

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
                    while(rand_val > prob[p] && p < prob.Length - 1) {
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

    private float getBranchScale(ref string branch_string) {
        Regex r = new Regex(@"^[0-9]*(\.[0-9]*)?");
        MatchCollection mc = r.Matches(branch_string);

        if(mc.Count == 0) {
            throw new System.Exception("Unable to find scale factor for branch [" + branch_string +
                "], please ensure that you specify a floating number immediately after the [ character to indicate scale");
        }

        float scale = float.Parse(mc[0].Value, CultureInfo.InvariantCulture.NumberFormat);
        branch_string = branch_string.Substring(mc[0].Value.Length);

        return scale;
    }

    private string getRotationCounters(string branch_string, ref int x_counter, ref int y_counter){
        int k = 0;
        bool rot_complete = false;
        while(k < branch_string.Length && !rot_complete) {
            switch(branch_string[k]) {
                case ROT_X_POS: 
                    x_counter++;
                    break;
                case ROT_X_NEG: 
                    x_counter--;
                    break;
                case ROT_Y_POS: 
                    y_counter++;
                    break;
                case ROT_Y_NEG: 
                    y_counter--;
                    break;
                default: rot_complete = true;
                    break;
            }

            if(!rot_complete) {
                k++;
            }
        }

        //strips rotation info
        return branch_string.Substring(k);
    }

    private void parseTreeString(string branch_string, GameObject parent) {
        float y_offset = 0;
        Mesh last_segment_mesh = null;

        int i = 0;
        while(i < branch_string.Length) {
            //process branch and recurse function
            if(branch_string[i] == BRANCH_START) {
                int openings = 1;
                int j = i + 1;

                //finds branch end
                for(; j < branch_string.Length; ++j) {
                    if(branch_string[j] == BRANCH_START) {
                        openings++;
                    }
                    else if(branch_string[j] == BRANCH_END) {
                        openings--;
                    }
   
                    if(openings == 0) {
                        break;
                    }
                    
                }

                if(j == branch_string.Length) {
                    throw new System.Exception("Parsing error, unable to find end of branch, check for missing ]");
                }

                //strips square brackets
                int length = j - i;
                string branch_substring = branch_string.Substring(i + 1, length - 1);
                
                float scale = getBranchScale(ref branch_substring);

                int x_rot_count = 0;
                int y_rot_count = 0;

                branch_substring = getRotationCounters(branch_substring, ref x_rot_count, ref y_rot_count);

                float x_rot = x_axis_rot_degree * (float)x_rot_count;
                float y_rot = y_axis_rot_degree * (float)y_rot_count;

                GameObject branch_parent = new GameObject();
                parseTreeString(branch_substring, branch_parent);

                branch_parent.transform.parent = parent.transform;
                branch_parent.transform.Rotate(new Vector3(x_rot, y_rot, 0));
                branch_parent.transform.localScale = new Vector3(scale, scale, scale);
                branch_parent.transform.localPosition = new Vector3(0, y_offset, 0);

                i = j + 1;  
            }
            //create desired branch segment
            else {
                if(!primitive_map.ContainsKey(branch_string[i])) {
                    throw new System.Exception("Cannot find primitive with name " + branch_string[i] + " when parsing branch string " + branch_string);
                }

                GameObject branch_segment = Instantiate(primitive_map[branch_string[i]].prefab);
                bool ignore_scale = primitive_map[branch_string[i]].ignore_scale;
                Mesh bs_mesh = branch_segment.GetComponent<MeshFilter>().mesh;
                Bounds mesh_bounds = bs_mesh.bounds;

                float y_offset_add = (mesh_bounds.max.y - mesh_bounds.min.y) * branch_segment.transform.localScale.y;
                float mesh_offset = mesh_bounds.min.y * branch_segment.transform.localScale.y;

                if(ignore_scale) {
                    GameObject scale_ignore = new GameObject();
                    branch_segment.transform.parent = scale_ignore.transform;
                    scale_ignore.transform.parent = parent.transform;
                }
                else {
                    branch_segment.transform.parent = parent.transform;
                }
                branch_segment.transform.localPosition = new Vector3(0, y_offset - mesh_offset, 0);

                last_segment_mesh = bs_mesh;

                y_offset += y_offset_add;

                i++;
            }
        }
    }

	// Use this for initialization
	void Start () {
        rng = new System.Random();
        primitive_map = new Dictionary<char, Primitive>();
        production_map = new Dictionary<char, List<ProductionRule>>();

        foreach(Primitive prim in primitives) {
            primitive_map[prim.name] = prim;
        }

        foreach(ProductionRule prod_rule in production_rules) {
            if(!primitive_map.ContainsKey(prod_rule.initial)) {
                throw new System.Exception("Production rule's initial value must also exist in the primitive list");
            }

            if(production_map.ContainsKey(prod_rule.initial)) {
                production_map[prod_rule.initial].Add(prod_rule);
            }
            else {
                List<ProductionRule> rule_list = new List<ProductionRule>();
                rule_list.Add(prod_rule);
                production_map[prod_rule.initial] = rule_list;
            }
        }

        tree_string = initial;
        for(int i = 0; i < iterations; ++i) {
            tree_string = rewrite(tree_string);
            Debug.Log(tree_string);
        }

        parseTreeString(tree_string, gameObject);
	}

    // Update is called once per frame
    void Update() {
        
	}
}
