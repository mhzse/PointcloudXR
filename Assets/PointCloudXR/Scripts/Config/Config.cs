/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;

namespace PointCloud.PointCloudConfig
{
    [Serializable]
    public class Config
    {
        public GeneratePoints config_generate_points;
        public Paths          paths;
    }

    [Serializable]
    public class GeneratePoints
    {
        public int   points_to_generate;
        public float points_spacing;
        public float points_size;
    }

    [Serializable]
    public class Paths
    {
        public string root_file_path;
    }
}
