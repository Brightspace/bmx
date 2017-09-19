from setuptools import setup

_PACKAGES = ['bmx']

_REQUIRES = [
        'future>=0.16.0',
        'lxml>=3.7.3',
        'okta>=0.0.4',
        'pies2overrides>=2.6.7',
        'prompt>=0.4.1',
        'requests>=2.18.3',
        'awscli>=1.11.0',
        'boto3>=1.4.7',
        'cerberus>=1.1''
]

setup(
    name='bmx',
    version='0.0.1',
    description='An AWS CLI wrapper that renews STS tokens',
    url='https://github.com/Brightspace/bmx',
    license='Proprietary',
    classifiers = [
	'Development Status :: 3 - Alpha',

    	'Intended Audience :: Developers',
    	'Topic :: Security',

	'License :: Other/Proprietary License'
    ],
    keywords='aws cli okta sts token credentials',
    packages=_PACKAGES,
    entry_points={
        'console_scripts': [
            'bmx=bmx.bmx:main'
        ]
    },
    install_requires=_REQUIRES,
    author='Chris Redekop',
    author_email='chris.redekop@d2l.com'
)
