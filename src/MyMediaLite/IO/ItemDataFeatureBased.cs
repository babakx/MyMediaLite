// Copyright (C) 2010, 2011, 2012 Zeno Gantner
// Copyright (C) 2015 Babak Loni
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using MyMediaLite.Data;
using MyMediaLite.DataType;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MyMediaLite.IO
{
    using FbPositiveFeedback = FeatureBasedPositiveFeedback<SparseBooleanMatrix>;

    /// <summary>Class that contains static methods for reading in implicit feedback data for ItemRecommender</summary>
    public static class ItemDataFeatureBased
    {
        /// <summary>Read in implicit feedback data from a file</summary>
        /// <param name="filename">name of the file to be read from</param>
        /// <param name="user_mapping">user <see cref="IMapping"/> object</param>
        /// <param name="item_mapping">item <see cref="IMapping"/> object</param>
        /// <param name="ignore_first_line">if true, ignore the first line</param>
        /// <returns>a <see cref="IPosOnlyFeedback"/> object with the user-wise collaborative data</returns>
        static public IPosOnlyFeedback Read(string filename, IMapping user_mapping = null, IMapping item_mapping = null, IMapping feature_mapping = null, bool ignore_first_line = false)
        {
            return Wrap.FormatException<IPosOnlyFeedback>(filename, delegate()
            {
                using (var reader = new StreamReader(filename))
                {
                    var feedback_data = (ISerializable)Read(reader, user_mapping, item_mapping, feature_mapping);

                    return (IPosOnlyFeedback)feedback_data;
                }
            });
        }

        /// <summary>Read in implicit feedback data from a TextReader</summary>
        /// <param name="reader">the TextReader to be read from</param>
        /// <param name="user_mapping">user <see cref="IMapping"/> object</param>
        /// <param name="item_mapping">item <see cref="IMapping"/> object</param>
        /// <param name="ignore_first_line">if true, ignore the first line</param>
        /// <returns>a <see cref="IPosOnlyFeedback"/> object with the user-wise collaborative data</returns>
        static public IPosOnlyFeedback Read(TextReader reader, IMapping user_mapping = null, IMapping item_mapping = null, IMapping feature_mapping = null, bool ignore_first_line = false)
        {
            if (user_mapping == null)
                user_mapping = new IdentityMapping();
            if (item_mapping == null)
                item_mapping = new IdentityMapping();
            if (feature_mapping == null)
                feature_mapping = new IdentityMapping();
            if (ignore_first_line)
                reader.ReadLine();

            var feedback = new FbPositiveFeedback();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().Length == 0)
                    continue;

                string[] tokens = line.Split(Constants.SPLIT_CHARS); 

                if (tokens.Length < 2)
                    throw new FormatException("Expected at least 2 columns: " + line);

                try
                {
                    int user_id = user_mapping.ToInternalID(tokens[1].Split(':')[0]);
                    int item_id = item_mapping.ToInternalID(tokens[2].Split(':')[0]);
                    feedback.Add(user_id, item_id);

                    int max_feature_id = FbPositiveFeedback.MaxFeatureId;
                    var features = new FeatureSet();
                    
                    for (int i = 3; i < tokens.Length; i++)
                    {
                        var parts = tokens[i].Split(':');
                        int feat_id = feature_mapping.ToInternalID(parts[0]);
                        if (feat_id > max_feature_id)
                            max_feature_id = feat_id;
                        
                        features.Add(feat_id, float.Parse(parts[1]));
                    }

                    FbPositiveFeedback.Features[user_id, item_id] = features;
                    FbPositiveFeedback.MaxFeatureId = max_feature_id;
                }
                catch (Exception)
                {
                    throw new FormatException(string.Format("Could not read line '{0}'", line));
                }
            }

            return feedback;
        }
    }
}