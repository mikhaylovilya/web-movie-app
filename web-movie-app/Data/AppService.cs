﻿using Microsoft.AspNetCore.Components;
using IMDbApiLib;
using IMDbApiLib.Models;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Newtonsoft.Json;
using System.Globalization;

namespace web_movie_app.Data
{
	public class AppService
	{
		internal string searchString { get; set; } = "";
		internal string currSearchString = "";

		internal QueryBuilder qb = new();
		internal List<Movie> movieList = new();
		internal Dictionary<Person, List<Category>> personDictionary = new();
		internal List<Tag> tagList = new();
		internal List<Movie> topMovieList = new();

		internal Dictionary<string, MovieData> movieData = new();
		internal Dictionary<string, string> personData = new();
		internal Dictionary<string, MovieData> topMovieData = new();

		internal Movie detailedMovie = new();
		internal MovieData detailedMovieData = new();
		internal Person detailedPerson = new();
		internal string detailedPersonData = "";
		internal Tag detailedTag = new();

		internal Dictionary<string, MovieData> cachedMovieData = new();
		internal Dictionary<string, string> cachedPersonData = new();

		internal string QueriesCountPath = "C:\\github\\web-movie-app\\web-movie-app\\QueriesCount.txt";
		internal async Task GetSearchReslults()
		{
			currSearchString = searchString;

			movieList = qb.MovieQuery(searchString);
			personDictionary = qb.PersonQuery(searchString);
			tagList = qb.TagQuery(searchString);

			var t = await GetMovieAndPersonData(movieList, personDictionary.Keys.ToList());

			movieData = t.Item1;
			personData = t.Item2;
		}
		internal async Task<Tuple<Dictionary<string, MovieData>, Dictionary<string, string>> > GetMovieAndPersonData(List<Movie> movieList, List<Person> personList)
		{
			int imdbQueriesCount = 0;
			int omdbQueriesCount = 0;

			var md = new Dictionary<string, MovieData>();
			var notCachedMovieList = new List<Movie>();
			var pd = new Dictionary<string, string>();
			var notCachedPersonList = new List<Person>();
			string tempDesc = "Description.aidsngasdfngsnadgljflsjdngadf;sj.gad;kngdsngndfnsgndksnnsngkdsfngldfskjnglsdkjfngkjdsfnlgndksfng";
			string tempImg = "aeroplane.jpg";

			foreach (Movie m in movieList)
			{
				if (cachedMovieData.ContainsKey(m.imdbId))
					md.Add(m.imdbId, cachedMovieData[m.imdbId]);
				else
					notCachedMovieList.Add(m);
			}
			foreach (Person p in personList)
			{
				if (cachedPersonData.ContainsKey(p.personId))
					pd.Add(p.personId, cachedPersonData[p.personId]);
				else 
					notCachedPersonList.Add(p);
			}

			var api = new ApiLib("k_6ax952gb");

			foreach (var m in notCachedMovieList)
			{
				//TitleData data = await api.TitleAsync(m.imdbId);
				//var tmpData = new MovieData { imageurl = tempImg, plot = tempDesc }/*{ imageurl = data.Image, plot = data.Plot }*/;
				OmdbApiMovie data = await GetOmdbMovieData(m.imdbId);
				MovieData tmpData;
				if (data.Poster != "N/A")
					tmpData = new MovieData { imageurl = data.Poster, plot = data.Plot };
				else
					tmpData = new MovieData { imageurl = tempImg, plot = data.Plot };
				md.Add(m.imdbId, tmpData);
				cachedMovieData.Add(m.imdbId, tmpData);

				omdbQueriesCount++;
			}
			foreach (var p in notCachedPersonList)
			{
				NameData data = await api.NameAsync(p.personId);
				string tmpData;
				if (data.Image != "")
					tmpData = /*tempImg*/ data.Image;
				else
					tmpData = tempImg;

				pd.Add(p.personId, tmpData);
				cachedPersonData.Add(p.personId, tmpData);

				imdbQueriesCount++;
			}

			await Task.Run(() => SerializeAmountOfQueriesToApi(imdbQueriesCount, omdbQueriesCount));
			return new Tuple<Dictionary<string, MovieData>, Dictionary<string, string>>(md, pd);
		}
		internal async Task GetTop10()
		{
			topMovieList = qb.GetTop10ByRating();
			var t = await GetMovieAndPersonData(topMovieList, new List<Person>());

			topMovieData = t.Item1;
		}

		internal async Task<OmdbApiMovie> GetOmdbMovieData(string movId)
		{
			string omdbKey = "783c5ac4";
			string url = "http://www.omdbapi.com/?i=" + movId + "&plot=full&apikey=" + omdbKey;

			var client = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Get, url);

			using (var response = await client.SendAsync(request)) 
			{
				if (response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					var res = JsonConvert.DeserializeObject<OmdbApiMovie>(json);
					if (res != null)
					{
						return res;
					}
					else
					{
						return new OmdbApiMovie();
					}
				}
			}

			return new OmdbApiMovie();
		}

		internal void SerializeAmountOfQueriesToApi(int imdbCount, int omdbCount)
		{
			int fileImdbCount = imdbCount;
			int fileOmdbCount = omdbCount;
			if (File.Exists(QueriesCountPath))
			{
				using (var reader = File.OpenText(QueriesCountPath))
				{
					try
					{
						fileImdbCount += int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
						fileOmdbCount += int.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);
					}
					catch { }
					reader.Close();
				}
			}
			using (var writer = new StreamWriter(QueriesCountPath, false))
			{
				writer.WriteLine(fileImdbCount.ToString());
				writer.WriteLine(fileOmdbCount.ToString());
				writer.Close();
			}
		}
	}
}
