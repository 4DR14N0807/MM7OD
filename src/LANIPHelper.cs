using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace MMXOnline;

public class LANIPHelper {
	object lockObj = new object();
	HashSet<string> ips = new HashSet<string>();
	string ipBase;

	public LANIPHelper() {
		string localIp = GetLocalIPAddress();
		var pieces = localIp.Split('.').ToList();
		pieces.Pop();
		ipBase = string.Join(".", pieces);
		ipBase += ".";
	}

	public static string GetLocalIPAddress() {
		string? localIP;
		try {
			using WebClient wc = new();
			localIP = wc.DownloadString("https://api.ipify.org/");
		} catch {
			return "127.0.0.1";
		}
		return localIP ?? "127.0.0.1";
	}

	public bool isLANIP(string ip) {
		return ip.StartsWith(ipBase) || ip == "127.0.0.1";
	}

	public List<string> getIps() {
		var ips = new List<string>();
		// Really really slow for anywhere near 200. Hence we only look for the first 20 ip's found on LAN.
		// Could investigate why more to make LAN ip lookup more automated
		for (int i = 1; i < 20; i++) {
			string ip = ipBase + i.ToString();
			if (ip.IsValidIpAddress()) {
				ips.Add(ip);
			}
		}
		return ips;
	}
}
