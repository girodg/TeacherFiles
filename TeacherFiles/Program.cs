using FSClassViewer;
using Microsoft.VisualBasic.FileIO;
using PG2Input;
using Syroot.Windows.IO;
using System;
using System.Collections.ObjectModel;
using static System.Collections.Specialized.BitVector32;

namespace TeacherFiles
{
    internal class Program
    {
        static void Main(string[] args)
        {

            List<string> menu = new List<string>()
            { "1. Select Course", "2. GitHub excel ", "3. Discord Roster", "4. IRs", "5. Course Director Awards", "6. Move Repos", "7. Attendance List", "8. Exit" };

            int selection;
            bool courseLoaded = false;
            string course = string.Empty;
            do
            {
                Console.Clear();
                if (courseLoaded)
                    Console.WriteLine($"Course: {course}");
                Input.GetMenuChoice("Files to generate? ", menu, out selection);
                Console.Clear();


                switch (selection)
                {
                    case 1:
                        students_.Clear();
                        course = LoadCourse();
                        courseLoaded = true;
                        break;
                    case 2:
                        if (!courseLoaded)
                        {
                            course = LoadCourse();
                            courseLoaded = true;
                        }
                        BuildRosterCSV(course);
                        Console.WriteLine("The file was created.");
                        Console.ReadKey();
                        break;
                    case 3:
                        if (!courseLoaded)
                        {
                            course = LoadCourse();
                            courseLoaded = true;
                        }
                        BuildDiscordRoster(course);
                        Console.WriteLine("The file was created.");
                        Console.ReadKey();
                        break;
                    case 4:
                        if (!courseLoaded)
                        {
                            course = LoadCourse();
                            courseLoaded = true;
                        }
                        string name = string.Empty;
                        Input.GetString("Enter the Course Director's name: ", ref name);
                        MakeFinalIR(course, name);
                        Console.WriteLine("The file was created.");
                        Console.ReadKey();
                        break;
                    case 5:
                        if (!courseLoaded)
                        {
                            course = LoadCourse();
                            courseLoaded = true;
                        }
                        MakeCDAFile(course);
                        Console.WriteLine("The file was created.");
                        Console.ReadKey();
                        break;

                    case 6:
                        if (!courseLoaded)
                        {
                            course = LoadCourse();
                            courseLoaded = true;
                        }
                        //
                        // NOTE: all of the repos must be downloaded to the appropriate C:\Repos folder
                        // FIRST: copy all of the repo names from the GitHub sheet to a txt file. EX: 2401.txt
                        //          In the file, enter a line like "SECTION:00" then after that line paste all of the URLs for the section
                        //        ****  Make sure to remove the directory info and any ".git" extensions  ****
                        // SECOND: Make sure you have the folders in the C:\Repos folder and the section folders in the month repo there
                        //          EX: C:\Repos\2401
                        //          EX: C:\Repos\2401\00
                        MoveRepos(course);
                        Console.WriteLine("Folders moved!");
                        Console.ReadKey();
                        break;

                    case 7:
                        if (!courseLoaded)
                        {
                            course = LoadCourse();
                            courseLoaded = true;
                        }
                        BuildAttendanceList(course);
                        Console.WriteLine("Attendance list generated!");
                        Console.ReadKey();
                        break;


                    default:
                        break;
                }

            } while (selection != menu.Count);
        }



        private static void BuildAttendanceList(string month)
        {
            string outFile = Path.Combine(KnownFolders.Downloads.Path, month + "_Attendance.csv");
            using (StreamWriter sw = new StreamWriter(outFile))
            {
                int numRosters = students_.Count;
                if(students_.TryGetValue("00", out List<Student> campus))
                {
                    foreach (var student in campus)
                    {
                        sw.WriteLine($"{student.LastName},{student.FirstName} {student.ID}");
                    }
                }

            }
        }

        private static void MoveRepos(string course)
        {
            int yr = DateTime.Now.Year - 2000;
            int mn = DateTime.Now.Month;
            string folder = $"{yr}{mn:D2}";
            string path = Path.Combine(@"C:\Repos", folder);
            var find = Directory.EnumerateDirectories(path, "*-submissions");
            if (find.Count() == 1)
            {
                DirectoryInfo di = new DirectoryInfo(find.ElementAt(0));
                string submissions = di.Name;// $"pg2-{folder}-submissions";

                //Console.WriteLine(path);
                Dictionary<string, List<string>> sections = new Dictionary<string, List<string>>();

                string inFile = Path.Combine(KnownFolders.Downloads.Path, folder + ".txt");
                using (StreamReader sr = new(inFile))
                {
                    int section = 0;
                    string destFolder = "00";
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("SECTION"))
                        {
                            string[] data = line.Split(':');
                            destFolder = data[1];
                            sections.TryAdd(destFolder, new List<string>());
                        }
                        else
                            sections[destFolder].Add(line);
                    }
                }
                string subPath = Path.Combine(path, submissions);
                string subRepoPath;
                foreach (var item in sections)
                {
                    string destPath = Path.Combine(path, item.Key);
                    foreach (var section in item.Value)
                    {
                        if (section.Count() > 0)
                        {
                            //Console.WriteLine($"{item.Key}: {section}");
                            subRepoPath = Path.Combine(subPath, section);
                            if (Path.Exists(subRepoPath))
                            {
                                string newDestPath = Path.Combine(destPath, section);
                                //Console.WriteLine($"{subRepoPath,-75} {newDestPath}");
                                Directory.Move(subRepoPath, newDestPath);
                            }
                        }
                    }
                }
            }
        }

        private static string LoadCourse()
        {
            string course = GetCourse();
            LoadAllRosters(course);
            return course;
        }

        private static void LoadAllRosters(string course)
        {
            rosters_ = GetRosters(course);
            foreach (var roster in rosters_)
            {
                //Console.WriteLine(roster);
                LoadStudents(roster);
                LoadGrades(roster);
            }
        }

        private static string GetCourse()
        {
            Console.WriteLine("Please enter a course. Some examples: 2401 or 2401_COP2334");
            string course = string.Empty;
            Input.GetString("Course: ", ref course);
            return course;
        }

        #region Methods


        private static void BuildDiscordRoster(string month)
        {
            string outFile = Path.Combine(KnownFolders.Downloads.Path, month + "_Discord.csv");
            using (StreamWriter sw = new StreamWriter(outFile))
            {
                foreach (var course in students_)
                {
                    foreach (var student in course.Value)
                    {
                        sw.WriteLine($"'{student.ID}',{student.FirstName},{student.LastName},,{student.PrimaryEmail}");
                    }
                }
            }
        }

        private static void BuildRosterCSV(string month)
        {
            string outFile = Path.Combine(KnownFolders.Downloads.Path, month + "_GitHub.csv");
            using (StreamWriter sw = new StreamWriter(outFile))
            {
                int numRosters = students_.Count;
                List<int> indexes = new List<int>(numRosters);
                for (int i = 0; i < numRosters; i++)
                {
                    indexes.Add(0);
                }
                bool moreStudents = true;
                while (moreStudents)
                {
                    moreStudents = false;

                    int rosterIndex = 0;
                    foreach (var roster in students_)
                    {
                        if (indexes[rosterIndex] < roster.Value.Count)
                        {
                            Student st = roster.Value[indexes[rosterIndex]];
                            sw.Write($"{st.FirstName},{st.LastName},,,");
                            indexes[rosterIndex]++;
                            moreStudents = true;
                        }
                        else
                        {
                            sw.Write(",,,,");
                        }
                        rosterIndex++;
                    }
                    sw.WriteLine();
                }
            }
        }

        private static void LoadStudents(string filePath)
        {
            // READ THE FILE
            string fileContent;
            using (StreamReader reader = new StreamReader(filePath))
            {
                fileContent = reader.ReadToEnd();
            }

            //SECTION
            string[] fileParts = filePath.Split('_');
            string section = fileParts[2];//sample name: C202203_COP2334-O_01_Roster
            List<Student> students = new List<Student>();
            students_.Add(section, students);

            // READ THE STUDENTS 
            string[] lines = fileContent.Split('\n');
            int index = 0;
            //Students.Clear();
            //List<string> students = new List<string>();
            string[] headerRow = lines[0].Split(',');
            int degreeIndex = Array.IndexOf(headerRow, "Degree Program");
            if (degreeIndex == -1) degreeIndex = 12;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    using (var parser = new TextFieldParser(new StringReader(line)))
                    {
                        parser.HasFieldsEnclosedInQuotes = true;
                        parser.Delimiters = new string[] { "," };
                        if (index == 0) //skip the header row
                        {
                            index++;
                            continue;
                        }
                        string[]? vals = parser.ReadFields();// line.Split(',');
                        if (vals != null)
                        {
                            char[] trims = new char[] { '\"' };
                            char[] trims2 = new char[] { '\'' };
                            if (vals[0].Length > 4) //then it's a student record
                            {
                                string id = vals[0].Trim(trims).Trim(trims2);
                                string name = $"{vals[1].Trim(trims)} {vals[2].Trim(trims)}";
                                Student nextStudent = new Student()
                                {
                                    ID = id,
                                    Name = name,
                                    FirstName = vals[1].Trim(trims),
                                    LastName = vals[2].Trim(trims),
                                    Degree = vals[degreeIndex].Trim(trims),
                                    PrimaryEmail = vals[4].Trim(trims),
                                    PersonalEmail = vals[5].Trim(trims),
                                    BestTime = vals[8],
                                    LastAccess = vals[9].Trim(trims2),
                                    IsOnline = filePath.Contains("-O"),
                                    Section = section
                                };
                                nextStudent.AddPhones(vals[6].Trim(trims));
                                nextStudent.AddPhones(vals[7].Trim(trims));
                                if (nextStudent.Degree.Contains(','))
                                {
                                    nextStudent.Degree = nextStudent.Degree.Substring(0, nextStudent.Degree.IndexOf(','));
                                }
                                nextStudent.Degree += $"{(nextStudent.IsOnline ? "-O" : "-L")}";
                                students.Add(nextStudent);
                            }
                        }
                    }
                }
            }
        }

        static void LoadGrades(string course)
        {
            string gradeFile = course.Replace("Roster", "Gradebook");
            if (!File.Exists(gradeFile))
            {
                Console.WriteLine("Cannot find the gradebook. Grades are not loaded.");
            }
            else
            {
                // READ THE FILE
                string fileContent;
                using (StreamReader reader = new StreamReader(gradeFile))
                {
                    fileContent = reader.ReadToEnd();
                }

                // READ THE STUDENTS 
                string[] lines = fileContent.Split('\n');
                int index = 0;

                List<Activity> activities = new List<Activity>();

                char[] trims = new char[] { '\"' };
                foreach (var line in lines)
                {
                    //the activity name row
                    //
                    if (index == 0)
                    {
                        //string[] names = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        using (var parser = new TextFieldParser(new StringReader(line)))
                        {
                            parser.HasFieldsEnclosedInQuotes = true;
                            parser.TrimWhiteSpace = true;
                            parser.Delimiters = new string[] { ",", ":" };
                            string[] names = parser.ReadFields().Where(x => !string.IsNullOrEmpty(x)).ToArray();// line.Split(',');
                            foreach (var name in names)
                            {
                                if (name != "Activity")
                                    activities.Add(new Activity() { Name = name.Trim(trims) });
                            }
                        }

                        index++;
                    }
                    //the activity weight row
                    //
                    else if (index == 1)
                    {
                        string[] weights = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < weights.Length; i++)
                        {
                            if (i > 0)
                            {
                                if (float.TryParse(weights[i], out float wt))
                                {
                                    activities[i - 1].Weight = wt;
                                }
                            }
                        }
                        index++;
                    }
                    else //the student grades
                    {
                        string[] fileParts = course.Split('_');
                        string section = fileParts[2];//sample name: C202203_COP2334-O_01_Roster
                        List<Student> students = students_[section];

                        string[] vals = line.Split(',');
                        List<string> professionalismNames = new List<string>() { "0.3 Professionalism", "0.7 Professionalism", "0.9 Professionalism" };
                        foreach (var student in students)
                        {
                            //find the student in the List of students
                            string valID = vals[0].Trim('\"').Trim('\'');
                            //Console.WriteLine(valID);
                            //Console.WriteLine(student.ID);
                            if (student.ID.Equals(valID))
                            {
                                student.Clear();
                                List<Activity> profActivities = new List<Activity>();
                                Activity professionalism = null;
                                for (int i = 2; i < vals.Length; i++) //skip the ID and name fields
                                {
                                    if (i == vals.Length - 1)
                                    {
                                        student.IsActive = vals[i].Equals("ACTIVE");
                                    }
                                    else if ((i - 2) < activities.Count && activities[i - 2].Weight > 0) //if(i == vals.Length - 2)
                                    {
                                        Activity cAct = activities[i - 2];
                                        Activity ac = new Activity() { Name = cAct.Name, Weight = cAct.Weight };
                                        ac.IsGraded = vals[i] != "-" && vals[i] != "C";
                                        ac.Grade = vals[i];
                                        student.Grades.Add(ac);
                                        //ac.PropertyChanged += student.WhatIf_PropertyChanged;
                                    }
                                }
                                student.CalculateGrades();
                                break;
                            }
                        }
                    }
                }
                //CalculateFailRate();
                //GradesLoaded = true;
                //if (showMessage)
                //    System.Windows.Forms.MessageBox.Show("The grades have been loaded.", "Load Grades");
            }
        }

        static void MakeCDAFile(string month)
        {
            string outFile = Path.Combine(KnownFolders.Downloads.Path, month + "_CDAs.txt");// Path.Combine(Path.GetDirectoryName(_filePath), $"finalIR_{Month}_{info.Shortcut}.txt");
            using (StreamWriter sw = new StreamWriter(outFile))
            {
                WriteCourseDirectorAwards(sw);
            }
        }

        static void WriteCourseDirectorAwards(StreamWriter sw)
        {

            foreach (var course in students_)
            {
                var CDA = course.Value.Where(x => x.IsActive == true && x.CurrentGrade >= 90).OrderByDescending(x => x.CurrentGrade).OrderBy(x => x.Section).ToList();
                sw.WriteLine($"Section {course.Key}:");

                foreach (var student in CDA)
                {
                    sw.WriteLine($"{student.CurrentGrade,6:N2} {student.Name} {student.ID}");
                }
            }

        }

        static void MakeFinalIR(string month, string courseDirector)
        {
            InitFailures();
            string outFile = Path.Combine(KnownFolders.Downloads.Path, month + "_IRs.txt");// Path.Combine(Path.GetDirectoryName(_filePath), $"finalIR_{Month}_{info.Shortcut}.txt");
            using (StreamWriter sw = new StreamWriter(outFile))
            {
                WriteFailureList(sw, courseDirector);
                //WriteCourseDirectorAwards(Students, sw, info);
            }
        }

        static void InitFailures(bool useCurrentGrade = false)
        {
            failingStudents_.Clear();
            foreach (var course in students_)
            {
                foreach (var student in course.Value)
                {
                    float grade = (useCurrentGrade) ? student.CurrentGrade : student.WorstGrade;
                    if (grade < Student.FailThreshold && student.IsActive && student.IsAudit == Auditing.No)
                    {
                        failingStudents_.Add(student);
                        if (student.FailureCount == Failures.None) student.FailureCount = Failures.First;
                    }
                }
            }
        }

        static void WriteFailureList(StreamWriter sw, string courseDirector)
        {

            var campusFirst = failingStudents_.Where(x => !x.IsOnline && x.FailureCount == Failures.First).ToList();
            var onlineFirst = failingStudents_.Where(x => x.IsOnline && x.FailureCount == Failures.First).ToList();

            //var campusMultiple = failingStudents_.Where(x => !x.IsOnline && x.FailureCount == Failures.Other).ToList();
            //var onlineMultiple = failingStudents_.Where(x => x.IsOnline && x.FailureCount == Failures.Other).ToList();

            if (campusFirst.Count > 0)
            {
                foreach (var student in campusFirst)
                {
                    sw.WriteLine($"{student.Name},{student.ID},CAMPUS,Section: {student.Section},{courseDirector}");
                }
            }
            if (onlineFirst.Count > 0)
            {
                foreach (var student in onlineFirst)
                {
                    sw.WriteLine($"{student.Name},{student.ID},ONLINE,Section: {student.Section},{courseDirector}");
                }
            }
        }

        private static List<string> GetRosters(string month)
        {
            string dwnLoads = KnownFolders.Downloads.Path;// (string.IsNullOrWhiteSpace(App.RosterRootPath)) ? KnownFolders.Downloads.Path : App.RosterRootPath;

            var rosters = Directory.EnumerateFiles(dwnLoads, "*_Roster.csv").ToList().FindAll((x) => { return x.Contains(month); });//.OrderByDescending(c => c).ToList();
            rosters.Sort(new RosterComparer());
            return rosters;
        }
        #endregion

        #region Fields
        static Dictionary<string, List<Student>> students_ = new Dictionary<string, List<Student>>();
        static List<string> rosters_ = new List<string>();
        static List<Student> failingStudents_ = new List<Student>();
        #endregion
    }


    public class RosterComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            int start = x.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            //compare the first parts: C202011 vs C202012
            string month1 = x.Substring(start, 7);
            string month2 = y.Substring(start, 7);

            int monthComp = month1.CompareTo(month2);
            if (monthComp != 0)
                return -monthComp;

            string rest1 = x.Substring(8);
            string rest2 = y.Substring(8);
            return rest1.CompareTo(rest2);
        }
    }
}