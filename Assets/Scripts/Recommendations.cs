using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Recommendations {
	public static ImageInfo[] interesting = new ImageInfo[] {
		new ImageInfo("http://www.flickr.com/photos/81504125@N00/17126948797/", "Dirk Wandel", "Tulpenland (360 x 180)", 17126948797L),
		new ImageInfo("http://www.flickr.com/photos/51035756831@N01/2091953040/", "Seb Przd", "Notre Dame de Reims in HDR", 2091953040L),
		new ImageInfo("http://www.flickr.com/photos/axlemasa/11028304606/", "Masao Nagata", "2013 Tokyo Motor Show", 11028304606L),
		new ImageInfo("http://www.flickr.com/photos/83248192@N00/767998948/", "HamburgerJung", "Diner in Duckingham Palace", 767998948L),
		new ImageInfo("http://www.flickr.com/photos/24183489@N00/4439644027/", "Alexandre Duret-Lutz", "Pont d'Iéna / Port de Suffren", 4439644027L),
		new ImageInfo("http://www.flickr.com/photos/tanjabarnes/21257032388/", "Tanja Barnes", "Bellagio Conservatory & Botanical Gardens", 21257032388L),
		new ImageInfo("http://www.flickr.com/photos/gporada/21142893594/", "gporada", "Interaktives Völkerschlachtsdenkmal Panorama 360", 21142893594L),
		new ImageInfo("http://www.flickr.com/photos/n-blueion/20494326394/", "Tomasz Szawkalo", "US-MA Fall River - 16inch shells", 20494326394L),
		new ImageInfo("http://www.flickr.com/photos/globalvoyager/21230969582/", "Nick Hobgood", "Welcome to Leleuvia island", 21230969582L),
		new ImageInfo("http://www.flickr.com/photos/83248192@N00/2059433934/", "HamburgerJung", "looking up", 2059433934L),
		new ImageInfo("http://www.flickr.com/photos/81504125@N00/10640316634/", "Dirk Wandel", "Aerosol - Arena / number one (360 x 180)", 10640316634L),
		new ImageInfo("http://www.flickr.com/photos/77581941@N00/7296157884/", "Masakazu Matsumoto", "Red gates", 7296157884L),
		new ImageInfo("http://www.flickr.com/photos/carlosmartindiaz/21550741049/", "Carlos Martin", "Sarlat Corner Equirectangular", 21550741049L),
		new ImageInfo("http://www.flickr.com/photos/24183489@N00/15637368122/", "Alexandre Duret-Lutz", "Ballon 3", 15637368122L),
		new ImageInfo("http://www.flickr.com/photos/64454994@N05/11635391344/", "Árni Jóhannsson", "seljalandsfoss_2_360_ff", 11635391344L),
		new ImageInfo("http://www.flickr.com/photos/77581941@N00/2811419692/", "Masakazu Matsumoto", "air-routes-1920", 2811419692L),
		new ImageInfo("http://www.flickr.com/photos/77581941@N00/2624015461/", "Masakazu Matsumoto", "Edge detection test", 2624015461L),
		new ImageInfo("http://www.flickr.com/photos/51035756831@N01/4622127629/", "Seb Przd", "Puppy, Guggenheim Museum in Bilbao", 4622127629L)
	};

	public static List<long> censored = new List<long>() {
		4565661045L,
		3648173484L,
		2704842426L,
		4566289016L
	};
}
