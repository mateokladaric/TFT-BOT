using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TFTCloudBot
{
	internal class Program
	{
		// getcursorpos pinvoke
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(out Point lpPoint);
		// mouse_event pinvoke
		[DllImport("user32.dll")]
		private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
		// getforegroundwindow pinvoke
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();
		// getwindowtext pinvoke
		[DllImport("user32.dll")]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
		// getasynckeystate pinvoke
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(Keys vKey);

		// mouse_event constants
		private const int MOUSEEVENTF_LEFTDOWN = 0x02;
		private const int MOUSEEVENTF_LEFTUP = 0x04;
		private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
		private const int MOUSEEVENTF_RIGHTUP = 0x10;
		private const int MOUSEEVENTF_MOVE = 0x0001;
		private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

		public static void MoveMouse(int x, int y)
		{
			double X = x * 65535 / Screen.PrimaryScreen.Bounds.Width;
			double Y = y * 65535 / Screen.PrimaryScreen.Bounds.Height;
			int currentX = Cursor.Position.X;
			int currentY = Cursor.Position.Y;
			int deltaX = x - currentX;
			int deltaY = y - currentY;
			int steps = 100;
			int delay = 10;
			int stepX = deltaX / steps;
			int stepY = deltaY / steps;

			for (int i = 0; i < steps; i++)
			{
				double t = (double)i / steps;
				double bezierX = Bezier(currentX, currentX + deltaX, t);
				double bezierY = Bezier(currentY, currentY + deltaY, t);
				X = bezierX * 65535 / Screen.PrimaryScreen.Bounds.Width;
				Y = bezierY * 65535 / Screen.PrimaryScreen.Bounds.Height;
				mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (int)X, (int)Y, 0, UIntPtr.Zero);
				System.Threading.Thread.Sleep(delay);
			}
		}

		public static void MouseClick()
		{
			System.Threading.Thread.Sleep(100);
			mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
			System.Threading.Thread.Sleep(20);
			mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
			System.Threading.Thread.Sleep(60);
		}

		private static double Bezier(double p0, double p1, double t) => (1 - t) * (1 - t) * (1 - t) * p0 + 3 * (1 - t) * (1 - t) * t * p0 + 3 * (1 - t) * t * t * p1 + t * t * t * p1;

		static void Main(string[] args)
		{

			// create a task for the cloud bot
			Task CloudBOT = new Task(() =>
			{
				// create a bitmap of the screen
				Bitmap screen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
				Graphics g = Graphics.FromImage(screen);
				// create a rectangle for the tactics button
				Rectangle tactics = new Rectangle(485, 926, 835, 2);
				Rectangle exp = new Rectangle(276, 913, 186, 5);
				Rectangle gold = new Rectangle(875, 888, 27, 17);
				// keybinds for the tactics button
				Keys DeployOrReturn = Keys.W;
				Keys BuyXP = Keys.F;
				Keys Reroll = Keys.D;
				Keys SellChampion = Keys.E;
				// temporary variables
				Color currentPixel;
				bool exit = false;
				// game loop
				while (true)
				{
					// if the foreground window is tft
					if (TFTCloud())
					{
						// take a screenshot
						g.CopyFromScreen(0, 0, 0, 0, screen.Size);
						// debug the current window
						Console.WriteLine("TFT Cloud");
						// check if a decision should be made
						if (IsInRound(screen))
						{
							// debug the current round
							Console.WriteLine("In round");
							// do all shop related actions
							//ShopBOT(screen, ref tactics, ref exit);
							// ocr gold and not exp
							if (GetAsyncKeyState(Keys.F1) < 0)
							{
								// save the gold rectangle from the screen
								Bitmap goldScreen = screen.Clone(gold, screen.PixelFormat);
								// save the gold rectangle to a file
								goldScreen.Save("gold.png");
							}
						}
						exit = false;
						System.Threading.Thread.Sleep(500);
					}
					else
					{
						// debug the current window
						Console.WriteLine("Not TFT Cloud");
						// wait for 2 seconds
						System.Threading.Thread.Sleep(2000);
					}
				}
			});
			CloudBOT.Start();
			CloudBOT.Wait();
		}

		private static void ShopBOT(Bitmap screen, ref Rectangle tactics, ref bool exit)
		{
			Color currentPixel;
			// check for tactics in the tactics rectangle
			for (int x = tactics.X; x < tactics.X + tactics.Width; x++)
			{
				for (int y = tactics.Y; y < tactics.Y + tactics.Height; y++)
				{
					// get the pixel at the current x and the tactics y
					currentPixel = screen.GetPixel(x, y);
					// if the pixel is the tactics color
					if (currentPixel.G > 120)
					{
						// click the tactics button
						MoveMouse(x, y + 10);
						MouseClick();
						exit = true;
						break;
					}
					// if the card is an upgrade
					if (currentPixel.R > 100 && currentPixel.G > 100 && currentPixel.B > 100)
					{
						// click the tactics button
						MoveMouse(x, tactics.Y + 10);
						MouseClick();
						exit = true;
						break;
					}
					// if the card is a triple upgrade
					if (currentPixel.R > 200 && currentPixel.G > 200 && currentPixel.B < 100)
					{
						// click the tactics button
						MoveMouse(x, tactics.Y + 10);
						MouseClick();
						exit = true;
						break;
					}
				}
				if (exit)
				{
					break;
				}
			}
		}

		private static bool IsInRound(Bitmap screen)
		{
			return screen.GetPixel(1111, 49) == Color.FromArgb(93, 209, 211);
		}

		private static bool TFTCloud()
		{
			// get the active window
			IntPtr handle = GetForegroundWindow();
			// get the title of the active window
			StringBuilder sb = new StringBuilder(256);
			GetWindowText(handle, sb, 256);
			// if the title is tft
			if (sb.ToString() == "League of Legends (TM) Client")
			{
				// return true
				return true;
			}
			// otherwise return false
			return false;
		}

		[Serializable]
		public class RNN
		{
			private double learningRate = 0.033;

			private List<List<double>> biases;
			private List<List<double>> errors;
			private List<List<double>> values;
			private List<List<List<double>>> weights;

			private double lastError = 0;

			private List<double> errorList = new List<double>();

			private Random r = new Random();

			public RNN(int[] layers)
			{
				layers[0] += layers[layers.Length - 1];
				initNeurons(layers);
				initWeights(layers);
			}

			private void initNeurons(int[] layers)
			{
				biases = new List<List<double>>();
				errors = new List<List<double>>();
				values = new List<List<double>>();

				for (int layerIdx = 0; layerIdx < layers.Length; layerIdx++)
				{
					biases.Add(new List<double>());
					errors.Add(new List<double>());
					values.Add(new List<double>());

					for (int neuronIdx = 0; neuronIdx < layers[layerIdx]; neuronIdx++)
					{
						biases[layerIdx].Add(0);
						errors[layerIdx].Add(0);
						values[layerIdx].Add(0);
					}
				}
			}

			private void initWeights(int[] layers)
			{
				weights = new List<List<List<double>>>();

				for (int layerIdx = 0; layerIdx < layers.Length - 1; layerIdx++)
				{
					weights.Add(new List<List<double>>());

					for (int neuronIdx = 0; neuronIdx < layers[layerIdx]; neuronIdx++)
					{
						weights[layerIdx].Add(new List<double>(layers[layerIdx + 1]));

						for (int nextNeuronIdx = 0; nextNeuronIdx < layers[layerIdx + 1]; nextNeuronIdx++)
						{
							weights[layerIdx][neuronIdx].Add((r.NextDouble() - 0.5f) * 2);
						}
					}
				}
			}

			private void activateTanh()
			{
				for (int layerIdx = 1; layerIdx < values.Count(); layerIdx++)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double sum = 0;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							sum += values[layerIdx - 1][prevNeuronIdx] * weights[layerIdx - 1][prevNeuronIdx][neuronIdx];
						}
						values[layerIdx][neuronIdx] = Math.Tanh(sum + biases[layerIdx][neuronIdx]);
					}
				}
			}

			private void activateReLU()
			{
				for (int layerIdx = 1; layerIdx < values.Count(); layerIdx++)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double sum = 0;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							sum += values[layerIdx - 1][prevNeuronIdx] * weights[layerIdx - 1][prevNeuronIdx][neuronIdx];
						}
						values[layerIdx][neuronIdx] = Math.Max(0, sum + biases[layerIdx][neuronIdx]);
					}
				}
			}

			private void activateLeakyReLU()
			{
				for (int layerIdx = 1; layerIdx < values.Count(); layerIdx++)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double sum = 0;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							sum += values[layerIdx - 1][prevNeuronIdx] * weights[layerIdx - 1][prevNeuronIdx][neuronIdx];
						}
						values[layerIdx][neuronIdx] = Math.Max(0.01 * sum, sum + biases[layerIdx][neuronIdx]);
					}
				}
			}

			private void activateSigmoid()
			{
				for (int layerIdx = 1; layerIdx < values.Count(); layerIdx++)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double sum = 0;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							sum += values[layerIdx - 1][prevNeuronIdx] * weights[layerIdx - 1][prevNeuronIdx][neuronIdx];
						}
						values[layerIdx][neuronIdx] = 1 / (1 + Math.Exp(-(sum + biases[layerIdx][neuronIdx])));
					}
				}
			}

			public double[] getTanhOutput(double[] inputs)
			{
				inputs = inputs.Concat(values.Last()).ToArray();

				System.Diagnostics.Contracts.Contract.Requires(inputs.Length == values[0].Count());

				for (int neuronIdx = 0; neuronIdx < inputs.Length; neuronIdx++)
				{
					values[0][neuronIdx] = inputs[neuronIdx];
				}

				activateTanh();

				return values[values.Count() - 1].ToArray();
			}

			public double[] getReLUOutput(double[] inputs)
			{
				inputs = inputs.Concat(values.Last()).ToArray();

				System.Diagnostics.Contracts.Contract.Requires(inputs.Length == values[0].Count());

				for (int neuronIdx = 0; neuronIdx < inputs.Length; neuronIdx++)
				{
					values[0][neuronIdx] = inputs[neuronIdx];
				}

				activateReLU();

				return values[values.Count() - 1].ToArray();
			}

			public double[] getLeakyReLUOutput(double[] inputs)
			{
				inputs = inputs.Concat(values.Last()).ToArray();

				System.Diagnostics.Contracts.Contract.Requires(inputs.Length == values[0].Count());

				for (int neuronIdx = 0; neuronIdx < inputs.Length; neuronIdx++)
				{
					values[0][neuronIdx] = inputs[neuronIdx];
				}

				activateLeakyReLU();

				return values[values.Count() - 1].ToArray();
			}

			public double[] getSigmoidOutput(double[] inputs)
			{
				inputs = inputs.Concat(values.Last()).ToArray();

				System.Diagnostics.Contracts.Contract.Requires(inputs.Length == values[0].Count());

				for (int neuronIdx = 0; neuronIdx < inputs.Length; neuronIdx++)
				{
					values[0][neuronIdx] = inputs[neuronIdx];
				}

				activateSigmoid();

				return values[values.Count() - 1].ToArray();
			}

			public void backPropagateTanh(double[] correctOutput)
			{
				for (int neuronIdx = 0; neuronIdx < values[values.Count() - 1].Count(); neuronIdx++)
				{
					errors[errors.Count() - 1][neuronIdx] = (correctOutput[neuronIdx] - values[values.Count() - 1][neuronIdx]) * (1 - values[values.Count() - 1][neuronIdx] * values[values.Count() - 1][neuronIdx]);
				}

				for (int layerIdx = values.Count() - 2; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double error = 0;
						for (int nextNeuronIdx = 0; nextNeuronIdx < values[layerIdx + 1].Count(); nextNeuronIdx++)
						{
							error += errors[layerIdx + 1][nextNeuronIdx] * weights[layerIdx][neuronIdx][nextNeuronIdx];
						}
						errors[layerIdx][neuronIdx] = error * (1 - values[layerIdx][neuronIdx] * values[layerIdx][neuronIdx]);
					}
				}

				for (int layerIdx = values.Count() - 1; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						biases[layerIdx][neuronIdx] += errors[layerIdx][neuronIdx] * learningRate;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							weights[layerIdx - 1][prevNeuronIdx][neuronIdx] += values[layerIdx - 1][prevNeuronIdx] * errors[layerIdx][neuronIdx] * learningRate;
						}
					}
				}
			}

			public void backPropagateReLU(double[] correctOutput)
			{
				for (int neuronIdx = 0; neuronIdx < values[values.Count() - 1].Count(); neuronIdx++)
				{
					errors[errors.Count() - 1][neuronIdx] = (correctOutput[neuronIdx] - values[values.Count() - 1][neuronIdx]) * (values[values.Count() - 1][neuronIdx] > 0 ? 1 : 0);
				}

				for (int layerIdx = values.Count() - 2; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double error = 0;
						for (int nextNeuronIdx = 0; nextNeuronIdx < values[layerIdx + 1].Count(); nextNeuronIdx++)
						{
							error += errors[layerIdx + 1][nextNeuronIdx] * weights[layerIdx][neuronIdx][nextNeuronIdx];
						}
						errors[layerIdx][neuronIdx] = error * (values[layerIdx][neuronIdx] > 0 ? 1 : 0);
					}
				}

				for (int layerIdx = values.Count() - 1; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						biases[layerIdx][neuronIdx] += errors[layerIdx][neuronIdx] * learningRate;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							weights[layerIdx - 1][prevNeuronIdx][neuronIdx] += values[layerIdx - 1][prevNeuronIdx] * errors[layerIdx][neuronIdx] * learningRate;
						}
					}
				}
			}

			public void backPropagateLeakyReLU(double[] correctOutput)
			{
				for (int neuronIdx = 0; neuronIdx < values[values.Count() - 1].Count(); neuronIdx++)
				{
					errors[errors.Count() - 1][neuronIdx] = (correctOutput[neuronIdx] - values[values.Count() - 1][neuronIdx]) * (values[values.Count() - 1][neuronIdx] > 0 ? 1 : 0.01);
				}

				for (int layerIdx = values.Count() - 2; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double error = 0;
						for (int nextNeuronIdx = 0; nextNeuronIdx < values[layerIdx + 1].Count(); nextNeuronIdx++)
						{
							error += errors[layerIdx + 1][nextNeuronIdx] * weights[layerIdx][neuronIdx][nextNeuronIdx];
						}
						errors[layerIdx][neuronIdx] = error * (values[layerIdx][neuronIdx] > 0 ? 1 : 0.01);
					}
				}

				for (int layerIdx = values.Count() - 1; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						biases[layerIdx][neuronIdx] += errors[layerIdx][neuronIdx] * learningRate;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							weights[layerIdx - 1][prevNeuronIdx][neuronIdx] += values[layerIdx - 1][prevNeuronIdx] * errors[layerIdx][neuronIdx] * learningRate;
						}
					}
				}
			}

			public void backPropagateSigmoid(double[] correctOutput)
			{
				for (int neuronIdx = 0; neuronIdx < values[values.Count() - 1].Count(); neuronIdx++)
				{
					errors[errors.Count() - 1][neuronIdx] = (correctOutput[neuronIdx] - values[values.Count() - 1][neuronIdx]) * values[values.Count() - 1][neuronIdx] * (1 - values[values.Count() - 1][neuronIdx]);
				}

				for (int layerIdx = values.Count() - 2; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						double error = 0;
						for (int nextNeuronIdx = 0; nextNeuronIdx < values[layerIdx + 1].Count(); nextNeuronIdx++)
						{
							error += errors[layerIdx + 1][nextNeuronIdx] * weights[layerIdx][neuronIdx][nextNeuronIdx];
						}
						errors[layerIdx][neuronIdx] = error * values[layerIdx][neuronIdx] * (1 - values[layerIdx][neuronIdx]);
					}
				}

				for (int layerIdx = values.Count() - 1; layerIdx > 0; layerIdx--)
				{
					for (int neuronIdx = 0; neuronIdx < values[layerIdx].Count(); neuronIdx++)
					{
						biases[layerIdx][neuronIdx] += errors[layerIdx][neuronIdx] * learningRate;
						for (int prevNeuronIdx = 0; prevNeuronIdx < values[layerIdx - 1].Count(); prevNeuronIdx++)
						{
							weights[layerIdx - 1][prevNeuronIdx][neuronIdx] += values[layerIdx - 1][prevNeuronIdx] * errors[layerIdx][neuronIdx] * learningRate;
						}
					}
				}
			}

			public void TrainTanh(double[] inputs, double[] correctOutput)
			{
				getTanhOutput(inputs);
				backPropagateTanh(correctOutput);
			}

			public void TrainReLU(double[] inputs, double[] correctOutput)
			{
				getReLUOutput(inputs);
				backPropagateReLU(correctOutput);
			}

			public void TrainLeakyReLU(double[] inputs, double[] correctOutput)
			{
				getLeakyReLUOutput(inputs);
				backPropagateLeakyReLU(correctOutput);
			}

			public void TrainSigmoid(double[] inputs, double[] correctOutput)
			{
				getSigmoidOutput(inputs);
				backPropagateSigmoid(correctOutput);
			}

			public void Save()
			{
				using (FileStream fileStream = new FileStream("brain.dat", FileMode.Create))
				{
					System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
					binaryFormatter.Serialize(fileStream, this);
				}
			}

			public void Load()
			{
				using (FileStream fileStream = new FileStream("brain.dat", FileMode.Open))
				{
					System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
					RNN brain = (RNN)binaryFormatter.Deserialize(fileStream);
					this.values = brain.values;
					this.weights = brain.weights;
					this.biases = brain.biases;
					this.errors = brain.errors;
				}
			}
		}
	}
}