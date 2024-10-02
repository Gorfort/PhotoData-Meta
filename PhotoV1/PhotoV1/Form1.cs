using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace PhotoV1
{
    public partial class Form1 : Form
    {
        private string currentFilePath = ""; // To keep track of the current image path

        public Form1()
        {
            InitializeComponent();

            // Enable Drag-and-Drop for panel1
            panel1.AllowDrop = true;

            // Set up DragEnter and DragDrop event handlers for panel1
            panel1.DragEnter += new DragEventHandler(panel1_DragEnter);
            panel1.DragDrop += new DragEventHandler(panel1_DragDrop);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        // Event: When a file is dragged over panel1
        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            // Allow the drag operation only if the data is in the form of files
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // Show that dropping is possible
            }
            else
            {
                e.Effect = DragDropEffects.None; // Disallow dropping if not a file
            }
        }

        // Event: When a file is dropped into panel1
        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            // Get the dropped file paths
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                currentFilePath = files[0]; // Store the path for use later

                // Check if the dropped file is an image
                if (IsImageFile(currentFilePath))
                {
                    // Optionally, display the image in panel1 (you can create a PictureBox)
                    DisplayImageInPanel1(currentFilePath);

                    // Get metadata of the image and display it in panel2
                    string metadata = GetImageMetadata(currentFilePath);
                    DisplayMetadataInPanel2(metadata);
                }
                else
                {
                    MessageBox.Show("Please drop an image file.", "Invalid File Type");
                }
            }
        }

        // Helper: Check if the file is an image (optional, based on extension)
        private bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp" || extension == ".gif" || extension == ".tiff";
        }

        // Helper: Display the image in panel1 (optional, you can modify how it is displayed)
        private void DisplayImageInPanel1(string filePath)
        {
            // Load the image
            Image image = Image.FromFile(filePath);

            // Create a PictureBox to show the image in panel1
            PictureBox pictureBox = new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill // Adjust to fit in panel1
            };

            // Clear any previous controls from panel1 before adding the image
            panel1.Controls.Clear();
            panel1.Controls.Add(pictureBox);
        }

        // Helper: Display the metadata in panel2
        private void DisplayMetadataInPanel2(string metadata)
        {
            // Clear any previous metadata controls from panel2
            panel2.Controls.Clear();

            // Use a multiline TextBox for better formatting of metadata
            TextBox metadataTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = metadata
            };

            // Add the metadata TextBox to panel2
            panel2.Controls.Add(metadataTextBox);
        }

        // Helper: Extract and return the image's metadata (ISO, Shutter Speed, Date Taken, Camera Model)
        private string GetImageMetadata(string filePath)
        {
            StringBuilder metadataInfo = new StringBuilder();
            try
            {
                using (Image image = Image.FromFile(filePath))
                {
                    PropertyItem[] properties = image.PropertyItems;

                    foreach (PropertyItem property in properties)
                    {
                        // ISO (Property ID: 0x8827)
                        if (property.Id == 0x8827)
                        {
                            int isoValue = BitConverter.ToUInt16(property.Value, 0);
                            metadataInfo.AppendLine($"ISO {isoValue}");
                        }

                        // Camera Manufacturer (Property ID: 0x010F)
                        if (property.Id == 0x010F)
                        {
                            string manufacturer = Encoding.ASCII.GetString(property.Value).Trim('\0');
                            metadataInfo.AppendLine($"      {manufacturer}");
                        }

                        // Camera Model (Property ID: 0x0110)
                        if (property.Id == 0x0110)
                        {
                            string cameraModel = Encoding.ASCII.GetString(property.Value).Trim('\0');
                            metadataInfo.AppendLine($"{cameraModel}");
                        }

                        // Shutter Speed (Property ID: 0x9201) - Stored as APEX value
                        if (property.Id == 0x9201)
                        {
                            var shutterSpeedValue = BitConverter.ToInt32(property.Value, 0);
                            var denominator = BitConverter.ToInt32(property.Value, 4);
                            if (denominator != 0)
                            {
                                double exposureTime = Math.Pow(2, -(double)shutterSpeedValue / denominator);
                                metadataInfo.AppendLine($"1/{Math.Round(1 / exposureTime)} sec");
                            }
                        }

                        // Date Taken (Property ID: 0x9003)
                        if (property.Id == 0x9003)
                        {
                            string dateTaken = Encoding.ASCII.GetString(property.Value).Trim();
                            // Split the date taken string and keep only the date (discarding time)
                            dateTaken = dateTaken.Split(' ')[0];
                            metadataInfo.AppendLine($"{dateTaken}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                metadataInfo.AppendLine($"Error reading metadata: {ex.Message}");
            }

            return metadataInfo.ToString();
        }


        // This is where we overlay metadata onto the image and save it
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                MessageBox.Show("Please drag and drop an image first.");
                return;
            }

            try
            {
                // Load the image
                using (Image image = Image.FromFile(currentFilePath))
                {
                    // Create a new bitmap with a black frame
                    int frameThickness = 20; // Thickness of the frame
                    Bitmap framedImage = new Bitmap(image.Width + frameThickness * 2, image.Height + frameThickness * 2);

                    using (Graphics g = Graphics.FromImage(framedImage))
                    {
                        // Fill the background with black color for the frame
                        g.Clear(Color.Black);

                        // Draw the original image onto the framed image
                        g.DrawImage(image, new Rectangle(frameThickness, frameThickness, image.Width, image.Height));

                        // Set up font and brush for drawing metadata
                        Font font = new Font("Arial", 10, FontStyle.Bold);
                        Brush orangeBrush = new SolidBrush(Color.Orange);

                        // Get the metadata
                        string metadata = GetImageMetadata(currentFilePath);
                        string metadataLine = string.Join(" ", metadata.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

                        // Draw the metadata on the framed image
                        int startX = frameThickness - 23; // Negative padding to move text inside the frame
                        int startY = frameThickness - 18; // Negative padding to move text inside the frame

                        // Draw the metadata within the frame
                        g.DrawString(metadataLine, font, orangeBrush, startX, startY);
                    }

                    // Save the edited image to a new file, ensuring high quality
                    string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "EditedImage.jpg");
                    var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L); // Set quality to 100

                    framedImage.Save(savePath, jpegEncoder, encoderParameters);

                    MessageBox.Show($"Image saved with metadata at: {savePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing the image: {ex.Message}");
            }
        }

        // Helper method to get the image encoder for JPEG format
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        // These Paint events can be ignored or used for custom painting
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
