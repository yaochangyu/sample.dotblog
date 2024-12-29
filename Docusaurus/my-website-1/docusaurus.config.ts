import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';
import type * as OpenApiPlugin from "docusaurus-plugin-openapi-docs"; // <--- 加這個
import path from "path"; // <--- 加這個
import fs from "fs"; // <--- 加這個
const generateOpenApiPlugins = () => { // <--- 加這個
  const openapiDir = path.resolve(__dirname, 'openapi');
  const openapiFiles = fs.readdirSync(openapiDir).filter(file => file.endsWith('.yml'));
  const config = openapiFiles.reduce((data, file) => {
    const id = path.basename(file, '.yml');
    console.log(`id: ${id}, file: ${file}`);
    data[id] = {
      specPath: path.join(openapiDir, file),
      outputDir: `docs/api/${id}`,
      sidebarOptions: {
        groupPathsBy: 'tag',
      }
    }  satisfies OpenApiPlugin.Options
    return data;
  }, {});
  const result = [
    'docusaurus-plugin-openapi-docs',
    {
      id: 'openapi',
      docsPluginId: 'classic',
      config,
    },
  ];
  console.log("result: " + result);
  return result;
};
const config: Config = {
  title: 'My Site',
  tagline: 'Dinosaurs are cool',
  favicon: 'img/favicon.ico',
  // Set the production url of your site here
  url: 'https://your-docusaurus-site.example.com',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',
  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'facebook', // Usually your GitHub org/user name.
  projectName: 'docusaurus', // Usually your repo name.
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/facebook/docusaurus/tree/main/packages/create-docusaurus/templates/shared/',
          docItemComponent: "@theme/ApiItem", //<--- 加這個
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ['rss', 'atom'],
            xslt: true,
          },
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/facebook/docusaurus/tree/main/packages/create-docusaurus/templates/shared/',
          // Useful options to enforce blogging best practices
          onInlineTags: 'warn',
          onInlineAuthors: 'warn',
          onUntruncatedBlogPosts: 'warn',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],
  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    navbar: {
      title: 'My Site',
      logo: {
        alt: 'My Site Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Tutorial',
        },
        {to: '/blog', label: 'Blog', position: 'left'},
        {
          href: 'https://github.com/facebook/docusaurus',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Tutorial',
              to: '/docs/intro',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Stack Overflow',
              href: 'https://stackoverflow.com/questions/tagged/docusaurus',
            },
            {
              label: 'Discord',
              href: 'https://discordapp.com/invite/docusaurus',
            },
            {
              label: 'X',
              href: 'https://x.com/docusaurus',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'Blog',
              to: '/blog',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/facebook/docusaurus',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} My Project, Inc. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
  plugins: [ // <--- 加這個
    generateOpenApiPlugins(),
  ],
  // plugins: [ // <--- 加這個
  //   [
  //     'docusaurus-plugin-openapi-docs',
  //     {
  //       id: "member",
  //       docsPluginId: "classic", // unique docsPluginId
  //       config: {
  //         member: {
  //           specPath: "openapi/member.yml",
  //           outputDir: "docs/api/member",
  //           sidebarOptions: {
  //             groupPathsBy: "tag",
  //           },
  //         } satisfies OpenApiPlugin.Options,
  //         product: {
  //           specPath: "openapi/product.yml",
  //           outputDir: "docs/api/product",
  //           sidebarOptions: {
  //             groupPathsBy: "tag",
  //           },
  //         } satisfies OpenApiPlugin.Options
  //       },
  //
  //     },
  //   ],
  // ],
  themes: ["docusaurus-theme-openapi-docs"], // <--- 加這個
};
export default config;