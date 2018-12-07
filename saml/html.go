package saml

import (
	"io"

	"golang.org/x/net/html"
)

func ParseHTML(r io.Reader) (string, error) {
	z := html.NewTokenizer(r)
	for {
		tt := z.Next()
		switch tt {
		case html.ErrorToken:
			return "", z.Err()
		case html.SelfClosingTagToken:
			tn, hasAttr := z.TagName()

			if string(tn) == "input" {
				attr := make(map[string]string)
				for hasAttr {
					key, val, moreAttr := z.TagAttr()
					attr[string(key)] = string(val)
					if !moreAttr {
						break
					}
				}

				if attr["name"] == "SAMLResponse" {
					return string(attr["value"]), nil
				}
			}
		}
	}
}
