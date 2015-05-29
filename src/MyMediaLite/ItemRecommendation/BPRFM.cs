using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyMediaLite.DataType;
using MyMediaLite.Taxonomy;
using MyMediaLite.IO;
using MyMediaLite.Data;

namespace MyMediaLite.ItemRecommendation
{
    using FbPositiveFeedback = FeatureBasedPositiveFeedback<SparseBooleanMatrix>;

    public class BPRFM : BPRMF
    {

        protected float w0;

        protected float[] w;

        protected Matrix<float> v;

        protected int num_features;

        /// <summary>Regularization parameter for auxiliary features</summary>
        public float RegC { get { return reg_c; } set { reg_c = value; } }
        /// <summary>Regularization parameter for auxiliary features</summary>
        protected float reg_c = 0.00025f;


        public MultiKeyDictionary<int, int, FeatureSet> Features { get; private set; }

        public int MaxFeatureId { get; private set; }

        public BPRFM() : base()
        { }

        protected override void InitModel()
        {
            Features = FbPositiveFeedback.Features;
            MaxFeatureId = FbPositiveFeedback.MaxFeatureId;

            num_features = MaxUserID + MaxItemID + MaxFeatureId + 1;
            
            v = new Matrix<float>(num_features, NumFactors);
            v.InitNormal(InitMean, InitStdDev);

            w0 = 0;
            w = new float[num_features];
        }


        protected override void UpdateFactors(int user_id, int item_id, int other_item_id, bool update_u, bool update_i, bool update_j)
        {
            int item_index = item_id + MaxUserID;
            other_item_id += MaxUserID;
            
            double y_uij = w[item_index] - w[other_item_id] + 
                DataType.MatrixExtensions.RowScalarProductWithRowDifference(v, user_id, v, item_index, v, other_item_id);

            foreach (var feat in Features[user_id, item_id])
            {
                int feat_index = feat.Key + MaxUserID + MaxItemID;
                y_uij += feat.Value * DataType.MatrixExtensions.RowScalarProductWithRowDifference(v, feat_index, v, item_index, v, other_item_id);
            }

            double one_over_one_plus_ex = 1 / (1 + Math.Exp(y_uij));

            // adjust bias terms
            if (update_i)
            {
                double update = one_over_one_plus_ex - BiasReg * w[item_index];
                w[item_index] += (float)(learn_rate * update);
            }

            if (update_j)
            {
                double update = -one_over_one_plus_ex - BiasReg * w[other_item_id];
                w[other_item_id] += (float)(learn_rate * update);
            }

            // adjust factors
            for (int f = 0; f < num_factors; f++)
            {
                float v_uf = v[user_id, f];
                float v_if = v[item_index, f];
                float v_jf = v[other_item_id, f];
                
                if (update_u)
                {
                    double update = (v_if - v_jf) * one_over_one_plus_ex - reg_u * v_uf;
                    v[user_id, f] = (float)(v_uf + learn_rate * update);
                }

                // term1 = Sum_{l=1}{num_features} c_l * v_{c_l,f}
                // term2 = Sum_{l=1}{num_features} c_l * v_{i,f} - v_{j,f}
                float term1 = 0f, term2 = 0f;

                foreach (var feat in Features[user_id, item_id])
                {
                    int feat_index = MaxUserID + MaxItemID + feat.Key;
                    term1 += feat.Value * v[feat_index, f];
                    term2 += feat.Value * (v_if - v_jf);
                }

                if (update_i)
                {
                    double update = (v_uf + term1) * one_over_one_plus_ex - reg_i * v_if;
                    v[item_index, f] = (float)(v_if + learn_rate * update);
                }

                if (update_j)
                {
                    double update = (-v_uf - term1) * one_over_one_plus_ex - reg_j * v_jf;
                    v[other_item_id, f] = (float)(v_jf + learn_rate * update);
                }

                foreach (var feat in Features[user_id, item_id])
                {
                    int feat_index = MaxUserID + MaxItemID + feat.Key;
                    double update = term2 * one_over_one_plus_ex - reg_c * v[feat_index, f];
                    v[feat_index, f] = (float)(v[feat_index, f] + learn_rate * update);
                }
            }
        }

        ///
        public override float Predict(int user_id, int item_id)
        {
            if (user_id > MaxUserID || item_id > MaxItemID)
                return float.MinValue;
            
            float term1 = 0, term2 = 0;
            
            if (Features.ContainsKey(user_id, item_id))
            {
                foreach (var feat in Features[user_id, item_id])
                {
                    int feat_index = MaxUserID + MaxItemID + feat.Key;

                    // if feat_index is greater than MaxFeatureId it means that the feature is new in test set so its factors has not been learnt
                    if (feat_index < num_features)
                    {
                        term1 += feat.Value * DataType.MatrixExtensions.RowScalarProduct(v, feat_index, v, user_id);
                        term2 += feat.Value * DataType.MatrixExtensions.RowScalarProduct(v, feat_index, v, MaxUserID + item_id);
                    }
                }
            }

            return w[MaxUserID + item_id] + term1 + term2 + DataType.MatrixExtensions.RowScalarProduct(v, user_id, v, MaxUserID + item_id);
        }

    }
}
