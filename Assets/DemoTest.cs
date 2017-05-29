// how to check IPv6 network before submit to AppStore
// https://developer.apple.com/library/content/documentation/NetworkingInternetWeb/Conceptual/NetworkingOverview/UnderstandingandPreparingfortheIPv6Transition/UnderstandingandPreparingfortheIPv6Transition.html#//apple_ref/doc/uid/TP40010220-CH213-SW1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System;

public class DemoTest : MonoBehaviour {

    enum ADDRESSFAM {
        IPv4, 
		IPv6
    }	

    [DllImport("__Internal")]
	private static extern string getIPv6 (string host);  
    [SerializeField]
	private string server;
	
	[SerializeField]
	private int port;	
	private Socket _socket;

    void Start () {
		Connect ();
    }

    string GetIPv6 (string host) {
        #if UNITY_IPHONE && !UNITY_EDITOR
		    return getIPv6 (host);
        #else
            return host + "&&ipv4";
        #endif
    }

	// Get IP type and synthesize IPv6, if needed, for iOS
    void GetIPType (string serverIp, out String newServerIp, out AddressFamily IPType) {
        IPType = AddressFamily.InterNetwork;
        newServerIp = serverIp;
        try {
            string IPv6 = GetIPv6 (serverIp);
            if (!string.IsNullOrEmpty (IPv6)) {
                string[] tmp = System.Text.RegularExpressions.Regex.Split (IPv6, "&&");
                if (tmp != null && tmp.Length >= 2) {
                    string type = tmp[1];
                    if (type == "ipv6") {
                        newServerIp = tmp[0];
                        IPType = AddressFamily.InterNetworkV6;
                    }
                }
            }
        } catch (Exception e) {
			Debug.LogErrorFormat ("GetIPv6 error: {0}", e.Message);
        }
    }

	// Get IP address by AddressFamily and domain
	private string GetIPAddress (string hostName, ADDRESSFAM AF) {
        if (AF == ADDRESSFAM.IPv6 && !System.Net.Sockets.Socket.OSSupportsIPv6)
            return null;
		if (string.IsNullOrEmpty (hostName))
			return null;
		System.Net.IPHostEntry host;
		string connectIP = "";
		try {
			host = System.Net.Dns.GetHostEntry (hostName);
			foreach (System.Net.IPAddress ip in host.AddressList) {
				if (AF == ADDRESSFAM.IPv4) {
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) 
						connectIP = ip.ToString ();
				}
				else if (AF == ADDRESSFAM.IPv6) {
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
						connectIP = ip.ToString ();
				}

			}
		} catch (Exception e) {
			Debug.LogErrorFormat ("GetIPAddress error: {0}", e.Message);
		}
		return connectIP;
    }

	// Check IP or not
	bool IsIPAddress (string data) {
		Match match = Regex.Match (data, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
		return match.Success;
	}

	// Connect to server
	// Detect IP or domain, convert and connect
	void Connect () {
		string connectionHost = server;
		string convertedHost = "";
		AddressFamily convertedFamily = AddressFamily.InterNetwork;			
		if (IsIPAddress (server)) {		
			GetIPType (server, out convertedHost, out convertedFamily);
			if (!string.IsNullOrEmpty (convertedHost))
				connectionHost = convertedHost;
		} else {
			convertedHost = GetIPAddress (server, ADDRESSFAM.IPv6);
			if (string.IsNullOrEmpty (convertedHost))			
				convertedHost = GetIPAddress (server, ADDRESSFAM.IPv4);
			else 
				convertedFamily = AddressFamily.InterNetworkV6;	
			if (string.IsNullOrEmpty (convertedHost)) {
				Debug.LogErrorFormat ("Can't get IP address");
				return;
			} else 
				connectionHost = convertedHost;
		}
		Debug.LogFormat ("Connecting to {0}, protocol {1}", connectionHost, convertedFamily);					
		_socket = new Socket (convertedFamily, SocketType.Stream, ProtocolType.Tcp);
		_socket.BeginConnect (connectionHost, port, new AsyncCallback (OnEndConnect), null);						
	}

	// Connection callback
	void OnEndConnect (IAsyncResult iar) {
		_socket.EndConnect (iar);
		Debug.Log ("Connected");
	}	



}