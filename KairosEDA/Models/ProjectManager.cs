using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace KairosEDA.Models
{
    public class Project
    {
        public string Name { get; set; } = "Untitled";
        public string Path { get; set; } = "";
        public List<string> RTLFiles { get; set; } = new List<string>();
        public string PDK { get; set; } = "Sky130";
        public Constraints Constraints { get; set; } = new Constraints();
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
        public List<BuildResult> BuildHistory { get; set; } = new List<BuildResult>();
        
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class Constraints
    {
        public double ClockPeriodNs { get; set; } = 10.0;  // 100 MHz default
        public double VoltageV { get; set; } = 1.8;
        public double PowerBudgetMw { get; set; } = 100.0;
        public double FloorplanWidthUm { get; set; } = 1000.0;
        public double FloorplanHeightUm { get; set; } = 1000.0;
        public double Utilization { get; set; } = 0.7;  // 70%
        public int RoutingLayers { get; set; } = 6;
        public string ClockPort { get; set; } = "clk";
    }

    public class BuildResult
    {
        public string Stage { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool Success { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }

    public class ProjectManager
    {
        public Project? CurrentProject { get; private set; }

        public void CreateNewProject(string name, string path)
        {
            CurrentProject = new Project
            {
                Name = name,
                Path = path
            };
        }

        public void LoadProject(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                CurrentProject = JsonConvert.DeserializeObject<Project>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load project: {ex.Message}");
            }
        }

        public void SaveProject()
        {
            if (CurrentProject == null)
                return;

            CurrentProject.LastModified = DateTime.Now;
            var json = JsonConvert.SerializeObject(CurrentProject, Formatting.Indented);
            
            var savePath = System.IO.Path.Combine(CurrentProject.Path, CurrentProject.Name + ".kproj");
            File.WriteAllText(savePath, json);
        }

        public void AddRTLFile(string filePath)
        {
            if (CurrentProject != null && !CurrentProject.RTLFiles.Contains(filePath))
            {
                CurrentProject.RTLFiles.Add(filePath);
            }
        }

        public void SetPDK(string pdk)
        {
            if (CurrentProject != null)
            {
                CurrentProject.PDK = pdk;
            }
        }
    }
}
