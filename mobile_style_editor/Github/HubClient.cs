﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Carto.Core;
using Octokit;

namespace mobile_style_editor
{
	public class HubClient
	{
		public static readonly HubClient Instance = new HubClient();

		public EventHandler<EventArgs> FileDownloadStarted;

		readonly GitHubClient client;

		/*
         * TODO - Improve authentication logic. Currently based on Personal Access Token
         * 
		 * Create your own personal access token at: https://github.com/settings/tokens/new
		 * and use it instead of your password to authenticate your account
		 * 
		 * I've included my own token in the assets, but not in the repository,
		 * so depending on your IDE, it may cause a compile-time or a runtime error
		 * unless you include the file in your solution as well
		 * 
		 * Because of the low rate limit for un-authenticated users, 
		 * authentication is necessary even when accessing public repositories:
         * https://developer.github.com/changes/2012-10-14-rate-limit-changes/
         * 
         * I've included sample code on the bottom of this page that you can uncomment and test for yourself
         * The repository is public, you should be able to do everything but final line, UpdateFile
		 */

		string Username = "<your-user-name>";
		string PAToken = "<your-personal-access-token>";

		HubClient()
		{
			Dictionary<string, string> dict = GetCredentials();
			Username = dict["username"];
			PAToken = dict["pa_token"];

			var credentials = new Credentials(Username, PAToken);

			client = new GitHubClient(new ProductHeaderValue("com.carto.style.editor"));
			client.Credentials = credentials;
		}

		public async Task<User> GetUser(string name)
		{
			return await client.User.Get(name);
		}

		public async Task<IReadOnlyList<Repository>> GetRepositories(string username)
		{
			return await client.Repository.GetAllForUser(username);
		}

		public Task<IReadOnlyList<RepositoryContent>> GetRepositoryContent(string owner, string name, string path = null)
		{
			if (path != null)
			{
				return client.Repository.Content.GetAllContents(owner, name, path);
			}

			return client.Repository.Content.GetAllContents(owner, name);
		}

		public async Task<DownloadedGithubFile> DownloadFile(RepositoryContent content)
		{
			string name = content.Name;
			string url = content.DownloadUrl.OriginalString;
			string path = content.Path;
			return await DownloadFile(name, url, path);
		}

		public async Task<DownloadedGithubFile> DownloadFile(GithubFile content)
		{
			string name = content.Name;
			string url = content.DownloadUrl;
			string path = content.Path;
			return await DownloadFile(name, url, path);
		}

		public async Task<DownloadedGithubFile> DownloadFile(string name, string url, string path)
		{
			DownloadedGithubFile result = new DownloadedGithubFile();;

			using (var client = new HttpClient())
			{
				HttpResponseMessage response = await client.GetAsync(url);

				result.Name = name;
				result.Path = path;
				result.Stream = await response.Content.ReadAsStreamAsync();
			}

			return result;
		}

		public async Task<List<DownloadedGithubFile>> DownloadFolder(string owner, string repoName, List<GithubFile> folder)
		{
			List<DownloadedGithubFile> files = new List<DownloadedGithubFile>();

			foreach (GithubFile file in folder)
			{
				if (file.IsDirectory)
				{
					string path = file.Path;
					var items = await GetRepositoryContent(owner, repoName, path);
					List<GithubFile> innerFolder = items.ToGithubFiles();
					List<DownloadedGithubFile> inner = await DownloadFolder(owner, repoName, innerFolder);
					files.AddRange(inner);
				}
				else
				{
					if (FileDownloadStarted != null)
					{
						FileDownloadStarted(file.Name, EventArgs.Empty);
					}

					Console.WriteLine("Downloading: " + file.Name + " (" + file.DownloadUrl + ")");
					DownloadedGithubFile downloaded = await DownloadFile(file.Name, file.DownloadUrl, file.Path);

					files.Add(downloaded);

				}
			}

			return files;
		}

		public async Task<bool> UpdateFile(Repository repository, RepositoryContent file, string content)
		{
			string url = file.DownloadUrl.AbsolutePath;
			string[] split = url.Split('/');

			/*
			 * TODO Splitting like this will not always yield positive results.
			 * e.g. it'll be longer if the file is in a subfolder
			 */
			string owner = split[1];
			string name = split[2];
			string branch = split[3];
			string path = split[4];

			// Both seem to work now, since the branch has been specified
			var request = new UpdateFileRequest("test upload from octokit api", content, file.Sha, branch);

			try
			{
				await client.Repository.Content.UpdateFile(owner, name, path, request);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
		}

		public Dictionary<string, string> GetCredentials()
		{
			var dictionary = new Dictionary<string, string>();
#if __UWP__
            Assembly assembly = typeof(Parser).GetTypeInfo().Assembly;
#else
			Assembly assembly = Assembly.GetAssembly(typeof(HubClient));
#endif
			string[] resources = assembly.GetManifestResourceNames();

			string name = "github_info.json";
			string path = null;

			foreach (var resource in resources)
			{
				if (resource.Contains(name) && !resource.Contains("with-params"))
				{
					path = resource;
				}
			}

			using (var stream = assembly.GetManifestResourceStream(path))
			{
				stream.Position = 0;
				using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
				{
					string result = reader.ReadToEnd();

					Variant variant = Variant.FromString(result);
					dictionary.Add("username", variant.GetObjectElement("username").String);
					dictionary.Add("pa_token", variant.GetObjectElement("pa_token").String);
				}
			}

			return dictionary;
		}

	}
}
