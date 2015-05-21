using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyMediaLite.DataType;
using MyMediaLite.Taxonomy;
using MyMediaLite.IO;

namespace MyMediaLite.ItemRecommendation
{
    public class BPRFM : BPRMF
    {

        protected float w0;

        protected float[] w;

        protected Matrix<float> v;

        protected int num_features;

        public BPRFM() : base()
        {
            //UpdateItems = true;
        }

        protected override void InitModel()
        {
            num_features = MaxUserID + MaxUserID + 1;
            
            v = new Matrix<float>(num_features, NumFactors);
            v.InitNormal(InitMean, InitStdDev);

            w0 = 0;
            w = new float[num_features];
        }


        protected virtual void UpdateFactors(int user_id, int item_id, int other_item_id, bool update_u, bool update_i, bool update_j)
        {
            item_id += MaxUserID;
            other_item_id += MaxUserID;
            
            double y_uij = w[item_id] - w[other_item_id] + 
                DataType.MatrixExtensions.RowScalarProductWithRowDifference(v, user_id, v, item_id, v, other_item_id);

            double one_over_one_plus_ex = 1 / (1 + Math.Exp(y_uij));

            // adjust bias terms
            if (update_i)
            {
                double update = one_over_one_plus_ex - BiasReg * w[item_id];
                w[item_id] += (float)(learn_rate * update);
            }

            if (update_j)
            {
                double update = -one_over_one_plus_ex - BiasReg * w[other_item_id];
                w[other_item_id] += (float)(learn_rate * update);
            }

            // adjust factors
            for (int f = 0; f < num_factors; f++)
            {
                float w_uf = v[user_id, f];
                float h_if = v[item_id, f];
                float h_jf = v[other_item_id, f];

                if (update_u)
                {
                    double update = (h_if - h_jf) * one_over_one_plus_ex - reg_u * w_uf;
                    v[user_id, f] = (float)(w_uf + learn_rate * update);
                }

                if (update_i)
                {
                    double update = w_uf * one_over_one_plus_ex - reg_i * h_if;
                    v[item_id, f] = (float)(h_if + learn_rate * update);
                }

                if (update_j)
                {
                    double update = -w_uf * one_over_one_plus_ex - reg_j * h_jf;
                    v[other_item_id, f] = (float)(h_jf + learn_rate * update);
                }
            }
        }

        ///
        public override float Predict(int user_id, int item_id)
        {
            if (user_id > MaxUserID || item_id > MaxItemID)
                return float.MinValue;

            return w[MaxUserID + item_id] + DataType.MatrixExtensions.RowScalarProduct(v, user_id, v, MaxUserID + item_id);
        }
    }
}
