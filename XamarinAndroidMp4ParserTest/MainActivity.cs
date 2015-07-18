using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content.Res;
using Android.OS;
using Com.Googlecode.Mp4parser;
using Com.Googlecode.Mp4parser.Authoring;
using Com.Googlecode.Mp4parser.Authoring.Builder;
using Com.Googlecode.Mp4parser.Authoring.Container.Mp4;
using Com.Googlecode.Mp4parser.Authoring.Tracks;
using Java.IO;
using Java.Nio.Channels;

namespace XamarinAndroidMp4Parser
{
	[Activity (Label = "XamarinAndroidMp4Parser", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);

			Start ();
		}

		private string _workingDirectory;

		private void Start() {
				
			_workingDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

			CreateSampleFile(Resource.Raw.cat1, _workingDirectory, "cat1.mp4");

			// This is just a merger, so the two videos need to have the same sizes and encoding
			Merge (_workingDirectory, "cat1.mp4", "cat1.mp4", _workingDirectory, "cat1-cat1.mp4");

			RemoveSampleFile (_workingDirectory, "cat1.mp4");

			// Note that you can use it along with other available Android API, or FFmpegLib, or FFmpeg, to mux files.
			// Mp4Parser is much more powerful than just merging files.
		}

		public void Mux(string videoTrackFile, string audioTrackFile, string destinationFolder, string destinationFile) {
			Task.Run (() => {

				var res = System.IO.File.Exists (destinationFolder + "/" + videoTrackFile);
				var h264Track = new H264TrackImpl (new FileDataSourceImpl (Path.Combine (destinationFolder, videoTrackFile)));
				var aacTrack = new AACTrackImpl (new FileDataSourceImpl (Path.Combine (destinationFolder, audioTrackFile)));

				var movie = new Movie ();
				movie.AddTrack (h264Track);
				movie.AddTrack (aacTrack);

				var mp4builder = new DefaultMp4Builder ();

				var mp4file = mp4builder.Build (movie);
				var fc = new FileOutputStream (new Java.IO.File (Path.Combine (destinationFolder, destinationFile))).Channel;
				mp4file.WriteContainer (fc);
				fc.Close ();
			});
		}

		// You can merge any amount of movies, but some format are less stable
		public void Merge(string sourceDirectory, string movie1, string movie2, string destinationDirectory, string destinationFile) {
			
			var inMovies = new Com.Googlecode.Mp4parser.Authoring.Movie[]{
				MovieCreator.Build(Path.Combine(sourceDirectory, movie1)),
				MovieCreator.Build(Path.Combine(sourceDirectory, movie2)),
			};

			var videoTracks = new List<ITrack>();
			var audioTracks = new List<ITrack>();

			foreach (var m in inMovies) {
				foreach (var t in m.Tracks) {
					if (t.Handler.Equals("soun")) {
						audioTracks.Add(t);
					}
					if (t.Handler.Equals("vide")) {
						videoTracks.Add(t);
					}
				}
			}

			var result = new Com.Googlecode.Mp4parser.Authoring.Movie();

			if (audioTracks.Count > 0) {
				result.AddTrack(new AppendTrack(audioTracks.ToArray()));
			}
			if (videoTracks.Count > 0) {
				result.AddTrack(new AppendTrack(videoTracks.ToArray()));
			}

			var outContainer = new DefaultMp4Builder().Build(result);

			var fc = new FileOutputStream(Path.Combine(destinationDirectory, destinationFile)).Channel;
			outContainer.WriteContainer(fc);
			fc.Close();
		}

		private void CreateSampleFile(int resource, string destinationFolder, string filename) {
			var data = new byte[0];
			using (var file = Resources.OpenRawResource (resource))
			using (var fileInMemory = new MemoryStream ()) {
				file.CopyTo (fileInMemory);
				data = fileInMemory.ToArray ();
			}
			var fileName = System.IO.Path.Combine (destinationFolder, filename);
			System.IO.File.WriteAllBytes (fileName, data);
		}

		void RemoveSampleFile(string sourceFolder, string name) {
			System.IO.File.Delete (System.IO.Path.Combine (sourceFolder, name));
		}
	}
}


