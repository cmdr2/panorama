﻿using UnityEngine;
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

	public static List<PanoramaImage> stereoImages = new List<PanoramaImage>() {
		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/Yofg2Lv.jpg",
			"http://i.imgur.com/Gl3qMf3.jpg",
			"http://i.imgur.com/eHTiy57.jpg",
			"http://code.blender.org/wp-content/uploads/2015/03/gooseberry_benchmark_panorama.jpg"
		}, StereoType.OVER_UNDER_INV),

		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/AnlxuJs.jpg",
			"http://i.imgur.com/etJOj9h.jpg",
			"http://i.imgur.com/DmMXc2B.jpg",
			"http://www.mediavr.com/chinesegarden1.jpg"
		}, StereoType.SBS),

		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/XJeYxCw.jpg",
			"http://i.imgur.com/W0D1iYM.jpg",
			"http://i.imgur.com/rVq1gUI.jpg"
		}, StereoType.OVER_UNDER),
		
		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/tPDUJiD.jpg",
			"http://i.imgur.com/K4XOLAr.jpg",
			"http://realvision.ae/blog/wp-content/uploads/2015/01/Maya_balcony_test_retinal_rivalry_reduced.jpg"
		}, StereoType.OVER_UNDER),
		
		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/s1Nngjr.jpg",
			"http://i.imgur.com/LIBtNCx.jpg",
			"http://i.ytimg.com/vi/LKsUxHASFBs/maxresdefault.jpg"
		}, StereoType.OVER_UNDER),
		
		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/B6r8MBG.jpg",
			"http://i.imgur.com/QB29MZv.jpg",
			"http://i.imgur.com/bz8njwi.jpg",
			"http://i.imgur.com/Ip4Yc7D.jpg",
			"http://i.imgur.com/bePKsgW.jpg"
		}, StereoType.OVER_UNDER),
		
		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/b8x0lRo.jpg",
			"http://i.imgur.com/6PYDgS1.jpg",
			"http://s10.postimg.org/ul4bea1i1/360_sbs_3_Dtest2_prev.jpg"
		}, StereoType.OVER_UNDER),
		
		new PanoramaImage(new List<string>() {
			"http://i.imgur.com/tufHhCF.jpg",
			"http://i.imgur.com/80YzqAE.jpg",
			"http://i.imgur.com/n0D9Isv.jpg",
			"http://i.imgur.com/Sc2H8yU.jpg",
			"http://realvision.ae/blog/wp-content/uploads/2014/11/Dirrogate_Airport_Stereoscopic_360_VR.jpg"
		}, StereoType.OVER_UNDER)
	};
}
